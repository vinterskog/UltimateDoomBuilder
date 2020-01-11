/*
**  BuilderNative Renderer
**  Copyright (c) 2019 Magnus Norddahl
**
**  This software is provided 'as-is', without any express or implied
**  warranty.  In no event will the authors be held liable for any damages
**  arising from the use of this software.
**
**  Permission is granted to anyone to use this software for any purpose,
**  including commercial applications, and to alter it and redistribute it
**  freely, subject to the following restrictions:
**
**  1. The origin of this software must not be misrepresented; you must not
**     claim that you wrote the original software. If you use this software
**     in a product, an acknowledgment in the product documentation would be
**     appreciated but is not required.
**  2. Altered source versions must be plainly marked as such, and must not be
**     misrepresented as being the original software.
**  3. This notice may not be removed or altered from any source distribution.
*/

#include "Precomp.h"
#include "VKRenderDevice.h"
#include "VKVertexBuffer.h"
#include "VKIndexBuffer.h"
#include "VKTexture.h"
#include "VKShaderManager.h"
#include "VKRenderPass.h"
#include "VKImageTransition.h"
#include "System/VulkanBuilders.h"
#include "System/VulkanSwapChain.h"
#include <stdexcept>
#include <cstdarg>
#include <algorithm>
#include <cmath>

namespace
{
	template<typename T> T clamp(T value, T minval, T maxval)
	{
		return std::max<T>(std::min<T>(value, maxval), minval);
	}
}

VKRenderDevice::VKRenderDevice(void* disp, void* window)
{
	Window = std::make_unique<Win32Window>(disp, window);
	Device = std::make_unique<VulkanDevice>(Window.get());

	swapChain = std::make_unique<VulkanSwapChain>(Device.get(), true);
	mSwapChainImageAvailableSemaphore.reset(new VulkanSemaphore(Device.get()));
	mRenderFinishedSemaphore.reset(new VulkanSemaphore(Device.get()));

	for (auto& semaphore : mSubmitSemaphore)
		semaphore.reset(new VulkanSemaphore(Device.get()));

	for (auto& fence : mSubmitFence)
		fence.reset(new VulkanFence(Device.get()));

	for (int i = 0; i < maxConcurrentSubmitCount; i++)
		mSubmitWaitFences[i] = mSubmitFence[i]->fence;

	mCommandPool.reset(new VulkanCommandPool(Device.get(), Device->graphicsFamily));

	mShaderManager = std::make_unique<VkShaderManager>(this);
	mRenderPassManager = std::make_unique<VkRenderPassManager>(this);
}

VKRenderDevice::~VKRenderDevice()
{
	ProcessDeleteList();
	for (VKTexture* tex : mTextures) mDeleteList.Textures.push_back(tex);
	for (VKIndexBuffer* buffer : mIndexBuffers) mDeleteList.IndexBuffers.push_back(buffer);
	for (VKVertexBuffer* buffer : mSharedVertexBuffers[0]->VertexBuffers) mDeleteList.VertexBuffers.push_back(buffer);
	for (VKVertexBuffer* buffer : mSharedVertexBuffers[1]->VertexBuffers) mDeleteList.VertexBuffers.push_back(buffer);
	ProcessDeleteList(true);
}

void VKRenderDevice::DeclareUniform(UniformName name, const char* glslname, UniformType type)
{
}

void VKRenderDevice::DeclareShader(ShaderName index, const char* name, const char* vertexshader, const char* fragmentshader)
{
	mShaderManager->DeclareShader(index, name, vertexshader, fragmentshader);
}

void VKRenderDevice::SetAlphaBlendEnable(bool value)
{
	if (mAlphaBlend != value)
	{
		mAlphaBlend = value;
		mNeedApply = true;
		mBlendStateChanged = true;
	}
}

void VKRenderDevice::SetAlphaTestEnable(bool value)
{
	if (mPipelineKey.AlphaTest != (int)value)
	{
		mPipelineKey.AlphaTest = value;
		mNeedApply = true;
	}
}

void VKRenderDevice::SetCullMode(Cull mode)
{
	if (mPipelineKey.CullMode != mode)
	{
		mPipelineKey.CullMode = mode;
		mNeedApply = true;
	}
}

void VKRenderDevice::SetBlendOperation(BlendOperation op)
{
	if (mBlendOperation != op)
	{
		mBlendOperation = op;
		mNeedApply = true;
		mBlendStateChanged = true;
	}
}

void VKRenderDevice::SetSourceBlend(Blend blend)
{
	if (mSourceBlend != blend)
	{
		mSourceBlend = blend;
		mNeedApply = true;
		mBlendStateChanged = true;
	}
}

void VKRenderDevice::SetDestinationBlend(Blend blend)
{
	if (mDestinationBlend != blend)
	{
		mDestinationBlend = blend;
		mNeedApply = true;
		mBlendStateChanged = true;
	}
}

void VKRenderDevice::SetFillMode(FillMode mode)
{
	if (mPipelineKey.Fill != mode)
	{
		mPipelineKey.Fill = mode;
		mNeedApply = true;
	}
}

void VKRenderDevice::SetMultisampleAntialias(bool value)
{
}

void VKRenderDevice::SetZEnable(bool value)
{
	if (mPipelineKey.DepthTest != (int)value)
	{
		mPipelineKey.DepthTest = value;
		mNeedApply = true;
	}
}

void VKRenderDevice::SetZWriteEnable(bool value)
{
	if (mPipelineKey.DepthWrite != (int)value)
	{
		mPipelineKey.DepthWrite = value;
		mNeedApply = true;
	}
}

void VKRenderDevice::SetVertexBuffer(VertexBuffer* ibuffer)
{
}

void VKRenderDevice::SetIndexBuffer(IndexBuffer* buffer)
{
}

void VKRenderDevice::SetTexture(Texture* texture)
{
}

void VKRenderDevice::SetSamplerFilter(TextureFilter minfilter, TextureFilter magfilter, MipmapFilter mipfilter, float maxanisotropy)
{
}

void VKRenderDevice::SetSamplerState(TextureAddress address)
{
}

bool VKRenderDevice::Draw(PrimitiveType type, int startIndex, int primitiveCount)
{
	static const int toVertexCount[] = { 2, 3, 1 };
	static const int toVertexStart[] = { 0, 0, 2 };

	if (mNeedApply)
		Apply(type);

	int vertexCount = toVertexStart[(int)type] + primitiveCount * toVertexCount[(int)type];
	mCommandBuffer->draw(vertexCount, 1, mVertexBufferStartIndex + startIndex, 0);

	return true;
}

bool VKRenderDevice::DrawIndexed(PrimitiveType type, int startIndex, int primitiveCount)
{
	static const int toVertexCount[] = { 2, 3, 1 };
	static const int toVertexStart[] = { 0, 0, 2 };

	if (mNeedApply)
		Apply(type);

	int vertexCount = toVertexStart[(int)type] + primitiveCount * toVertexCount[(int)type];
	mCommandBuffer->drawIndexed(vertexCount, 1, startIndex, mVertexBufferStartIndex, 0);

	return true;
}

bool VKRenderDevice::DrawData(PrimitiveType type, int startIndex, int primitiveCount, const void* data)
{
	return true;
}

bool VKRenderDevice::StartRendering(bool clear, int backcolor, Texture* target, bool usedepthbuffer)
{
	BeginRenderPass(clear, backcolor, target, usedepthbuffer);

	mViewportX = 0;
	mViewportY = 0;
	mViewportWidth = mRenderTarget.Width;
	mViewportHeight = mRenderTarget.Height;
	mViewportChanged = true;

	mScissorX = 0;
	mScissorY = 0;
	mScissorWidth = mRenderTarget.Width;
	mScissorHeight = mRenderTarget.Height;
	mScissorChanged = true;

	return true;
}

bool VKRenderDevice::FinishRendering()
{
	EndRenderPass();
	return true;
}

bool VKRenderDevice::Present()
{
	FlushCommands(true, true);
	return true;
}

bool VKRenderDevice::ClearTexture(int backcolor, Texture* texture)
{
	return true;
}

bool VKRenderDevice::CopyTexture(Texture* idst, CubeMapFace face)
{
	return true;
}

bool VKRenderDevice::SetVertexBufferData(VertexBuffer* ibuffer, void* data, int64_t size, VertexFormat format)
{
	return true;
}

bool VKRenderDevice::SetVertexBufferSubdata(VertexBuffer* ibuffer, int64_t destOffset, void* data, int64_t size)
{
	return true;
}

bool VKRenderDevice::SetIndexBufferData(IndexBuffer* ibuffer, void* data, int64_t size)
{
	return true;
}

bool VKRenderDevice::SetPixels(Texture* itexture, const void* data)
{
	return true;
}

bool VKRenderDevice::SetCubePixels(Texture* itexture, CubeMapFace face, const void* data)
{
	return true;
}

void* VKRenderDevice::MapPBO(Texture* itexture)
{
	return nullptr;
}

bool VKRenderDevice::UnmapPBO(Texture* itexture)
{
	return true;
}

void VKRenderDevice::SetShader(ShaderName name)
{
	if (mPipelineKey.Shader != name)
	{
		mPipelineKey.Shader = name;
		mNeedApply = true;
	}
}

void VKRenderDevice::SetUniform(UniformName name, const void* values, int count, int bytesize)
{
}

void VKRenderDevice::Apply(PrimitiveType drawtype)
{
	mApplyCount++;
	if (mApplyCount >= 1000)
	{
		FlushCommands(false);
		mApplyCount = 0;
	}

	ApplyBlendState();
	ApplyPipeline(drawtype);
	ApplyVertexBuffer();
	ApplyIndexBuffer();
	ApplyScissor();
	ApplyViewport();
	ApplyStencilRef();
	ApplyDepthBias();
	ApplyUniformSet();
	ApplyPushConstants();
	ApplyMaterial();
	mNeedApply = false;
}

void VKRenderDevice::ApplyBlendState()
{
	if (mBlendStateChanged)
	{
		if (mAlphaBlend)
		{
			mPipelineKey.BlendOp = mBlendOperation;
			mPipelineKey.SrcBlend = mSourceBlend;
			mPipelineKey.DestBlend = mDestinationBlend;
		}
		else
		{
			mPipelineKey.BlendOp = BlendOperation::Add;
			mPipelineKey.SrcBlend = Blend::One;
			mPipelineKey.DestBlend = Blend::Zero;
		}

		mBlendStateChanged = false;
	}
}

void VKRenderDevice::ApplyPipeline(PrimitiveType drawtype)
{
	mPipelineKey.DrawType = drawtype;

	if (mNeedPipeline || mBoundPipelineKey != mPipelineKey)
	{
		mCommandBuffer->bindPipeline(VK_PIPELINE_BIND_POINT_GRAPHICS, mPassSetup->GetPipeline(mPipelineKey));
		mBoundPipelineKey = mPipelineKey;
	}
}

void VKRenderDevice::ApplyViewport()
{
	if (mViewportChanged)
	{
		VkViewport viewport;
		if (mViewportWidth >= 0)
		{
			viewport.x = (float)mViewportX;
			viewport.y = (float)mViewportY;
			viewport.width = (float)mViewportWidth;
			viewport.height = (float)mViewportHeight;
		}
		else
		{
			viewport.x = 0.0f;
			viewport.y = 0.0f;
			viewport.width = (float)mRenderTarget.Width;
			viewport.height = (float)mRenderTarget.Height;
		}
		viewport.minDepth = mViewportDepthMin;
		viewport.maxDepth = mViewportDepthMax;
		mCommandBuffer->setViewport(0, 1, &viewport);
		mViewportChanged = false;
	}
}

void VKRenderDevice::ApplyScissor()
{
	if (mScissorChanged)
	{
		VkRect2D scissor;
		if (mScissorWidth >= 0)
		{
			int x0 = clamp(mScissorX, 0, mRenderTarget.Width);
			int y0 = clamp(mScissorY, 0, mRenderTarget.Height);
			int x1 = clamp(mScissorX + mScissorWidth, 0, mRenderTarget.Width);
			int y1 = clamp(mScissorY + mScissorHeight, 0, mRenderTarget.Height);

			scissor.offset.x = x0;
			scissor.offset.y = y0;
			scissor.extent.width = x1 - x0;
			scissor.extent.height = y1 - y0;
		}
		else
		{
			scissor.offset.x = 0;
			scissor.offset.y = 0;
			scissor.extent.width = mRenderTarget.Width;
			scissor.extent.height = mRenderTarget.Height;
		}
		mCommandBuffer->setScissor(0, 1, &scissor);
		mScissorChanged = false;
	}
}

void VKRenderDevice::ApplyDepthBias()
{
	if (mDepthBiasChanged)
	{
		//mCommandBuffer->setDepthBias(mDepthBiasUnits, 0.0f, mDepthBiasFactor);
		mDepthBiasChanged = false;
	}
}

void VKRenderDevice::ApplyStencilRef()
{
	if (mStencilRefChanged)
	{
		//mCommandBuffer->setStencilReference(VK_STENCIL_FRONT_AND_BACK, mStencilRef);
		mStencilRefChanged = false;
	}
}

void VKRenderDevice::ApplyVertexBuffer()
{
	/*if (mVertexBuffer != mLastVertexBuffer || mVertexOffset != mLastVertexOffset)
	{
		const VkVertexFormat* format = mRenderPassManager->GetVertexFormat(mVertexBuffer->VertexFormat);
		VkBuffer vertexBuffer = mVertexBuffer->buffer;
		VkDeviceSize offset = 0;
		mCommandBuffer->bindVertexBuffers(0, 1, &vertexBuffer, &offset);
		mLastVertexBuffer = mVertexBuffer;
		mLastVertexOffset = mVertexOffset;
	}*/
}

void VKRenderDevice::ApplyIndexBuffer()
{
	/*if (mIndexBuffer != mLastIndexBuffer && mIndexBuffer)
	{
		mCommandBuffer->bindIndexBuffer(static_cast<VKIndexBuffer*>(mIndexBuffer)->mBuffer->buffer, 0, VK_INDEX_TYPE_UINT32);
		mLastIndexBuffer = mIndexBuffer;
	}*/
}

void VKRenderDevice::ApplyMaterial()
{
	if (mMaterialChanged)
	{
		//mCommandBuffer->bindDescriptorSet(VK_PIPELINE_BIND_POINT_GRAPHICS, passManager->GetPipelineLayout(mPipelineKey.NumTextureLayers), 1, mMaterial->GetDescriptorSet());
		mMaterialChanged = false;
	}
}

void VKRenderDevice::ApplyUniformSet()
{
}

void VKRenderDevice::ApplyPushConstants()
{
}

std::mutex& VKRenderDevice::GetMutex()
{
	static std::mutex m;
	return m;
}

void VKRenderDevice::DeleteObject(VKVertexBuffer* buffer)
{
	std::unique_lock<std::mutex> lock(VKRenderDevice::GetMutex());
	if (buffer->Device)
		buffer->Device->mDeleteList.VertexBuffers.push_back(buffer);
	else
		delete buffer;
}

void VKRenderDevice::DeleteObject(VKIndexBuffer* buffer)
{
	std::unique_lock<std::mutex> lock(VKRenderDevice::GetMutex());
	if (buffer->Device)
		buffer->Device->mDeleteList.IndexBuffers.push_back(buffer);
	else
		delete buffer;
}

void VKRenderDevice::DeleteObject(VKTexture* texture)
{
	std::unique_lock<std::mutex> lock(VKRenderDevice::GetMutex());
	if (texture->Device)
		texture->Device->mDeleteList.Textures.push_back(texture);
	else
		delete texture;
}

void VKRenderDevice::ProcessDeleteList(bool finalize)
{
	std::unique_lock<std::mutex> lock(VKRenderDevice::GetMutex());

	if (!finalize)
	{
		for (auto buffer : mDeleteList.IndexBuffers) delete buffer;
		for (auto buffer : mDeleteList.VertexBuffers) delete buffer;
		for (auto texture : mDeleteList.Textures) delete texture;
	}
	else
	{
		for (auto buffer : mDeleteList.IndexBuffers) buffer->Finalize();
		for (auto buffer : mDeleteList.VertexBuffers) buffer->Finalize();
		for (auto texture : mDeleteList.Textures) texture->Finalize();
	}

	mDeleteList.IndexBuffers.clear();
	mDeleteList.VertexBuffers.clear();
	mDeleteList.Textures.clear();
}

VulkanCommandBuffer* VKRenderDevice::GetTransferCommands()
{
	if (!mTransferCommands)
	{
		mTransferCommands = mCommandPool->createBuffer();
		mTransferCommands->SetDebugName("VKRenderDevice.mTransferCommands");
		mTransferCommands->begin();
	}
	return mTransferCommands.get();
}

VulkanCommandBuffer* VKRenderDevice::GetDrawCommands()
{
	if (!mDrawCommands)
	{
		mDrawCommands = mCommandPool->createBuffer();
		mDrawCommands->SetDebugName("VKRenderDevice.mDrawCommands");
		mDrawCommands->begin();
	}
	return mDrawCommands.get();
}

void VKRenderDevice::DeleteFrameObjects()
{
	FrameDeleteList.Images.clear();
	FrameDeleteList.ImageViews.clear();
	FrameDeleteList.Framebuffers.clear();
	FrameDeleteList.Buffers.clear();
	FrameDeleteList.Descriptors.clear();
	FrameDeleteList.DescriptorPools.clear();
	FrameDeleteList.CommandBuffers.clear();
}

void VKRenderDevice::FlushCommands(VulkanCommandBuffer** commands, size_t count, bool finish, bool lastsubmit)
{
	int currentIndex = mNextSubmit % maxConcurrentSubmitCount;

	if (mNextSubmit >= maxConcurrentSubmitCount)
	{
		vkWaitForFences(Device->device, 1, &mSubmitFence[currentIndex]->fence, VK_TRUE, std::numeric_limits<uint64_t>::max());
		vkResetFences(Device->device, 1, &mSubmitFence[currentIndex]->fence);
	}

	QueueSubmit submit;

	for (size_t i = 0; i < count; i++)
		submit.addCommandBuffer(commands[i]);

	if (mNextSubmit > 0)
		submit.addWait(VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT, mSubmitSemaphore[(mNextSubmit - 1) % maxConcurrentSubmitCount].get());

	if (finish && presentImageIndex != 0xffffffff)
	{
		submit.addWait(VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT, mSwapChainImageAvailableSemaphore.get());
		submit.addSignal(mRenderFinishedSemaphore.get());
	}

	if (!lastsubmit)
		submit.addSignal(mSubmitSemaphore[currentIndex].get());

	submit.execute(Device.get(), Device->graphicsQueue, mSubmitFence[currentIndex].get());
	mNextSubmit++;
}

void VKRenderDevice::FlushCommands(bool finish, bool lastsubmit)
{
	EndRenderPass();

	if (mDrawCommands || mTransferCommands)
	{
		VulkanCommandBuffer* commands[2];
		size_t count = 0;

		if (mTransferCommands)
		{
			mTransferCommands->end();
			commands[count++] = mTransferCommands.get();
			FrameDeleteList.CommandBuffers.push_back(std::move(mTransferCommands));
		}

		if (mDrawCommands)
		{
			mDrawCommands->end();
			commands[count++] = mDrawCommands.get();
			FrameDeleteList.CommandBuffers.push_back(std::move(mDrawCommands));
		}

		FlushCommands(commands, count, finish, lastsubmit);
	}
}

void VKRenderDevice::WaitForCommands(bool finish)
{
	if (finish)
	{
		presentImageIndex = swapChain->acquireImage(GetClientWidth(), GetClientHeight(), mSwapChainImageAvailableSemaphore.get());
		if (presentImageIndex != 0xffffffff)
			DrawPresentTexture();
	}

	FlushCommands(finish, true);

	if (finish && presentImageIndex != 0xffffffff)
	{
		swapChain->queuePresent(presentImageIndex, mRenderFinishedSemaphore.get());
	}

	int numWaitFences = std::min(mNextSubmit, (int)maxConcurrentSubmitCount);
	if (numWaitFences > 0)
	{
		vkWaitForFences(Device->device, numWaitFences, mSubmitWaitFences, VK_TRUE, std::numeric_limits<uint64_t>::max());
		vkResetFences(Device->device, numWaitFences, mSubmitWaitFences);
	}

	DeleteFrameObjects();
	mNextSubmit = 0;
}

void VKRenderDevice::BeginRenderPass(bool clear, int backcolor, Texture* target, bool usedepthbuffer)
{
	EndRenderPass();

	mCommandBuffer = GetDrawCommands();

	if (target)
	{
		VKTexture* vktarget = static_cast<VKTexture*>(target);

		mRenderTarget.Image = vktarget->Image.get();
		mRenderTarget.DepthStencil = usedepthbuffer ? vktarget->DepthStencil->View.get() : nullptr;
		mRenderTarget.Width = vktarget->GetWidth();
		mRenderTarget.Height = vktarget->GetHeight();
	}
	else
	{
		mRenderTarget.Image = mSceneBuffers.Image.get();
		mRenderTarget.DepthStencil = usedepthbuffer ? mSceneBuffers.DepthStencil->View.get() : nullptr;
		mRenderTarget.Width = mSceneBuffers.Width;
		mRenderTarget.Height = mSceneBuffers.Height;
	}

	VkRenderPassKey key = {};
	key.DrawBufferFormat = VK_FORMAT_R8G8B8A8_UNORM;
	key.Samples = VK_SAMPLE_COUNT_1_BIT;
	key.DepthStencil = usedepthbuffer;
	key.DepthStencilFormat = VK_FORMAT_D32_SFLOAT;

	mPassSetup = mRenderPassManager->GetRenderPass(key);

	auto& framebuffer = mRenderTarget.Image->RSFramebuffers[key];
	if (!framebuffer)
	{
		FramebufferBuilder builder;
		builder.setRenderPass(mPassSetup->GetRenderPass(false, false, false));
		builder.setSize(mRenderTarget.Width, mRenderTarget.Height);
		builder.addAttachment(mRenderTarget.Image->View.get());
		if (key.DepthStencil)
			builder.addAttachment(mRenderTarget.DepthStencil);
		framebuffer = builder.create(Device.get());
		framebuffer->SetDebugName("VkRenderPassSetup.Framebuffer");
	}

	RenderPassBegin beginInfo;
	beginInfo.setRenderPass(mPassSetup->GetRenderPass(clear, clear && usedepthbuffer, false));
	beginInfo.setRenderArea(0, 0, mRenderTarget.Width, mRenderTarget.Height);
	beginInfo.setFramebuffer(framebuffer.get());
	beginInfo.addClearColor(RPART(backcolor) / 255.0f, GPART(backcolor) / 255.0f, BPART(backcolor) / 255.0f, APART(backcolor) / 255.0f);
	beginInfo.addClearDepthStencil(1.0f, 0);
	mCommandBuffer->beginRenderPass(beginInfo);

	mNeedPipeline = true;
	mScissorChanged = true;
	mViewportChanged = true;
}

void VKRenderDevice::EndRenderPass()
{
	if (mCommandBuffer)
	{
		mCommandBuffer->endRenderPass();
		mCommandBuffer = nullptr;
		mBoundPipelineKey = {};
	}
}

void VKRenderDevice::DrawPresentTexture()
{
}

int VKRenderDevice::GetClientWidth()
{
	RECT box = { 0 };
	GetClientRect((HWND)Window->window, &box);
	return box.right - box.left;
}

int VKRenderDevice::GetClientHeight()
{
	RECT box = { 0 };
	GetClientRect((HWND)Window->window, &box);
	return box.bottom - box.top;
}
