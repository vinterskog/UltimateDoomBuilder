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
#include "System/VulkanBuilders.h"
#include "System/VulkanSwapChain.h"
#include <stdexcept>
#include <cstdarg>
#include <algorithm>
#include <cmath>

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

void VKRenderDevice::SetVertexBuffer(VertexBuffer* ibuffer)
{
}

void VKRenderDevice::SetIndexBuffer(IndexBuffer* buffer)
{
}

void VKRenderDevice::SetAlphaBlendEnable(bool value)
{
}

void VKRenderDevice::SetAlphaTestEnable(bool value)
{
}

void VKRenderDevice::SetCullMode(Cull mode)
{
}

void VKRenderDevice::SetBlendOperation(BlendOperation op)
{
}

void VKRenderDevice::SetSourceBlend(Blend blend)
{
}

void VKRenderDevice::SetDestinationBlend(Blend blend)
{
}

void VKRenderDevice::SetFillMode(FillMode mode)
{
}

void VKRenderDevice::SetMultisampleAntialias(bool value)
{
}

void VKRenderDevice::SetZEnable(bool value)
{
}

void VKRenderDevice::SetZWriteEnable(bool value)
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
	return true;
}

bool VKRenderDevice::DrawIndexed(PrimitiveType type, int startIndex, int primitiveCount)
{
	return true;
}

bool VKRenderDevice::DrawData(PrimitiveType type, int startIndex, int primitiveCount, const void* data)
{
	return true;
}

bool VKRenderDevice::StartRendering(bool clear, int backcolor, Texture* itarget, bool usedepthbuffer)
{
	return true;
}

bool VKRenderDevice::FinishRendering()
{
	return true;
}

bool VKRenderDevice::Present()
{
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
}

void VKRenderDevice::SetUniform(UniformName name, const void* values, int count, int bytesize)
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

void VKRenderDevice::EndRenderPass()
{
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
