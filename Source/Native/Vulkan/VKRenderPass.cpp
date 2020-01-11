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
#include "VKRenderPass.h"
#include "VKRenderDevice.h"
#include "VKShaderManager.h"
#include "System/VulkanBuilders.h"

VkRenderPassManager::VkRenderPassManager(VKRenderDevice* fb) : fb(fb)
{
}

VkRenderPassManager::~VkRenderPassManager()
{
	UniformSet.reset(); // Needed since it must come before destruction of UniformDescriptorPool
}

void VkRenderPassManager::Init()
{
	CreateUniformSetLayout();
	CreateDescriptorPool();
	CreateUniformSet();
}

void VkRenderPassManager::RenderBuffersReset()
{
	RenderPassSetup.clear();
}

void VkRenderPassManager::TextureSetPoolReset()
{
	auto& deleteList = fb->FrameDeleteList;

	for (auto& desc : TextureDescriptorPools)
	{
		deleteList.DescriptorPools.push_back(std::move(desc));
	}

	TextureDescriptorPools.clear();
	TextureDescriptorSetsLeft = 0;
	TextureDescriptorsLeft = 0;
}

VkRenderPassSetup* VkRenderPassManager::GetRenderPass(const VkRenderPassKey& key)
{
	auto& item = RenderPassSetup[key];
	if (!item)
		item.reset(new VkRenderPassSetup(fb, key));
	return item.get();
}

int VkRenderPassManager::GetVertexFormat(int numBindingPoints, int numAttributes, size_t stride, const VertexBufferAttribute* attrs)
{
	for (size_t i = 0; i < VertexFormats.size(); i++)
	{
		const auto& f = VertexFormats[i];
		if (f.Attrs.size() == (size_t)numAttributes && f.NumBindingPoints == numBindingPoints && f.Stride == stride)
		{
			bool matches = true;
			for (int j = 0; j < numAttributes; j++)
			{
				if (memcmp(&f.Attrs[j], &attrs[j], sizeof(VertexBufferAttribute)) != 0)
				{
					matches = false;
					break;
				}
			}

			if (matches)
				return (int)i;
		}
	}

	VkVertexFormat fmt;
	fmt.NumBindingPoints = numBindingPoints;
	fmt.Stride = stride;
	fmt.Attrs.insert(fmt.Attrs.end(), attrs, attrs + numAttributes);
	VertexFormats.push_back(fmt);
	return (int)VertexFormats.size() - 1;
}

VkVertexFormat* VkRenderPassManager::GetVertexFormat(int index)
{
	return &VertexFormats[index];
}

void VkRenderPassManager::CreateUniformSetLayout()
{
	DescriptorSetLayoutBuilder builder;
	builder.addBinding(0, VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, 1, VK_SHADER_STAGE_VERTEX_BIT | VK_SHADER_STAGE_FRAGMENT_BIT);
	builder.addBinding(1, VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, 1, VK_SHADER_STAGE_VERTEX_BIT | VK_SHADER_STAGE_FRAGMENT_BIT);
	UniformSetLayout = builder.create(fb->Device.get());
	UniformSetLayout->SetDebugName("VkRenderPassManager.UniformSetLayout");
}

VulkanDescriptorSetLayout* VkRenderPassManager::GetTextureSetLayout(int numLayers)
{
	if (TextureSetLayouts.size() < (size_t)numLayers)
		TextureSetLayouts.resize(numLayers);

	auto& layout = TextureSetLayouts[numLayers - 1];
	if (layout)
		return layout.get();

	DescriptorSetLayoutBuilder builder;
	for (int i = 0; i < numLayers; i++)
	{
		builder.addBinding(i, VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, 1, VK_SHADER_STAGE_FRAGMENT_BIT);
	}
	layout = builder.create(fb->Device.get());
	layout->SetDebugName("VkRenderPassManager.TextureSetLayout");
	return layout.get();
}

VulkanPipelineLayout* VkRenderPassManager::GetPipelineLayout(int numLayers)
{
	if (PipelineLayouts.size() <= (size_t)numLayers)
		PipelineLayouts.resize(numLayers + 1);

	auto& layout = PipelineLayouts[numLayers];
	if (layout)
		return layout.get();

	PipelineLayoutBuilder builder;
	builder.addSetLayout(UniformSetLayout.get());
	if (numLayers != 0)
		builder.addSetLayout(GetTextureSetLayout(numLayers));
	//builder.addPushConstantRange(VK_SHADER_STAGE_VERTEX_BIT | VK_SHADER_STAGE_FRAGMENT_BIT, 0, sizeof(PushConstants));
	layout = builder.create(fb->Device.get());
	layout->SetDebugName("VkRenderPassManager.PipelineLayout");
	return layout.get();
}

void VkRenderPassManager::CreateDescriptorPool()
{
	DescriptorPoolBuilder builder;
	builder.addPoolSize(VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, 2);
	builder.setMaxSets(1);
	UniformDescriptorPool = builder.create(fb->Device.get());
	UniformDescriptorPool->SetDebugName("VkRenderPassManager.UniformDescriptorPool");
}

void VkRenderPassManager::CreateUniformSet()
{
	UniformSet = UniformDescriptorPool->allocate(UniformSetLayout.get());
	if (!UniformSet)
		throw std::runtime_error("CreateUniformSet failed");
}

void VkRenderPassManager::UpdateUniformSet()
{
	/*WriteDescriptors update;
	update.addBuffer(UniformSet.get(), 0, VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, fb->MatrixBuffer->UniformBuffer->mBuffer.get(), 0, sizeof(MatricesUBO));
	update.addBuffer(UniformSet.get(), 1, VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, fb->StreamBuffer->UniformBuffer->mBuffer.get(), 0, sizeof(StreamUBO));
	update.updateSets(fb->Device.get());*/
}

std::unique_ptr<VulkanDescriptorSet> VkRenderPassManager::AllocateTextureDescriptorSet(int numLayers)
{
	if (TextureDescriptorSetsLeft == 0 || TextureDescriptorsLeft < numLayers)
	{
		TextureDescriptorSetsLeft = 1000;
		TextureDescriptorsLeft = 2000;

		DescriptorPoolBuilder builder;
		builder.addPoolSize(VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, TextureDescriptorsLeft);
		builder.setMaxSets(TextureDescriptorSetsLeft);
		TextureDescriptorPools.push_back(builder.create(fb->Device.get()));
		TextureDescriptorPools.back()->SetDebugName("VkRenderPassManager.TextureDescriptorPool");
	}

	TextureDescriptorSetsLeft--;
	TextureDescriptorsLeft -= numLayers;
	return TextureDescriptorPools.back()->allocate(GetTextureSetLayout(numLayers));
}

/////////////////////////////////////////////////////////////////////////////

VkRenderPassSetup::VkRenderPassSetup(VKRenderDevice* fb, const VkRenderPassKey& key) : PassKey(key), fb(fb)
{
}

std::unique_ptr<VulkanRenderPass> VkRenderPassSetup::CreateRenderPass(bool clearColor, bool clearDepth, bool clearStencil)
{
	RenderPassBuilder builder;

	builder.addAttachment(
		PassKey.DrawBufferFormat, (VkSampleCountFlagBits)PassKey.Samples,
		clearColor ? VK_ATTACHMENT_LOAD_OP_CLEAR : VK_ATTACHMENT_LOAD_OP_LOAD, VK_ATTACHMENT_STORE_OP_STORE,
		VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL);

	if (PassKey.DepthStencil)
	{
		builder.addDepthStencilAttachment(
			PassKey.DepthStencilFormat, (VkSampleCountFlagBits)PassKey.Samples,
			clearDepth ? VK_ATTACHMENT_LOAD_OP_CLEAR : VK_ATTACHMENT_LOAD_OP_LOAD, VK_ATTACHMENT_STORE_OP_STORE,
			clearStencil ? VK_ATTACHMENT_LOAD_OP_CLEAR : VK_ATTACHMENT_LOAD_OP_LOAD, VK_ATTACHMENT_STORE_OP_STORE,
			VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL, VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL);
	}

	builder.addSubpass();

	if (PassKey.DepthStencil)
	{
		builder.addSubpassDepthStencilAttachmentRef(1, VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL);
		builder.addExternalSubpassDependency(
			VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
			VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
			VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT | VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
			VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT | VK_ACCESS_COLOR_ATTACHMENT_READ_BIT);
	}
	else
	{
		builder.addExternalSubpassDependency(
			VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
			VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT,
			VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT,
			VK_ACCESS_COLOR_ATTACHMENT_READ_BIT);
	}

	auto renderpass = builder.create(fb->Device.get());
	renderpass->SetDebugName("VkRenderPassSetup.RenderPass");
	return renderpass;
}

VulkanRenderPass* VkRenderPassSetup::GetRenderPass(bool clearColor, bool clearDepth, bool clearStencil)
{
	int key = (((int)clearColor) << 2) | (((int)clearDepth) << 1) | (int)clearStencil;
	if (!RenderPasses[key])
		RenderPasses[key] = CreateRenderPass(clearColor, clearDepth, clearStencil);
	return RenderPasses[key].get();
}

VulkanPipeline* VkRenderPassSetup::GetPipeline(const VkPipelineKey& key)
{
	auto& item = Pipelines[key];
	if (!item)
		item = CreatePipeline(key);
	return item.get();
}

std::unique_ptr<VulkanPipeline> VkRenderPassSetup::CreatePipeline(const VkPipelineKey& key)
{
	GraphicsPipelineBuilder builder;

	VkShaderProgram* program = fb->GetShaderManager()->Get(key.Shader, key.AlphaTest);
	builder.addVertexShader(program->vert.get());
	builder.addFragmentShader(program->frag.get());

	const VkVertexFormat& vfmt = *fb->GetRenderPassManager()->GetVertexFormat(key.VertexFormat);

	for (int i = 0; i < vfmt.NumBindingPoints; i++)
		builder.addVertexBufferBinding(i, vfmt.Stride);

	bool inputLocations[6] = { false, false, false, false, false, false };

	for (size_t i = 0; i < vfmt.Attrs.size(); i++)
	{
		const auto& attr = vfmt.Attrs[i];
		builder.addVertexAttribute(attr.location, attr.binding, attr.format, attr.offset);
		inputLocations[attr.location] = true;
	}

	// Vulkan requires an attribute binding for each location specified in the shader
	for (int i = 0; i < 6; i++)
	{
		if (!inputLocations[i])
			builder.addVertexAttribute(i, 0, VK_FORMAT_R32G32B32_SFLOAT, 0);
	}

	builder.addDynamicState(VK_DYNAMIC_STATE_VIEWPORT);
	builder.addDynamicState(VK_DYNAMIC_STATE_SCISSOR);
	// builder.addDynamicState(VK_DYNAMIC_STATE_LINE_WIDTH);
	builder.addDynamicState(VK_DYNAMIC_STATE_DEPTH_BIAS);
	// builder.addDynamicState(VK_DYNAMIC_STATE_BLEND_CONSTANTS);
	// builder.addDynamicState(VK_DYNAMIC_STATE_DEPTH_BOUNDS);
	// builder.addDynamicState(VK_DYNAMIC_STATE_STENCIL_COMPARE_MASK);
	// builder.addDynamicState(VK_DYNAMIC_STATE_STENCIL_WRITE_MASK);
	builder.addDynamicState(VK_DYNAMIC_STATE_STENCIL_REFERENCE);

	// Note: the actual values are ignored since we use dynamic viewport+scissor states
	builder.setViewport(0.0f, 0.0f, 320.0f, 200.0f);
	builder.setScissor(0, 0, 320.0f, 200.0f);

	static const VkPrimitiveTopology vktopology[] = {
		VK_PRIMITIVE_TOPOLOGY_LINE_LIST,
		VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST,
		VK_PRIMITIVE_TOPOLOGY_TRIANGLE_STRIP
	};

	static const VkStencilOp op2vk[] = { VK_STENCIL_OP_KEEP, VK_STENCIL_OP_INCREMENT_AND_CLAMP, VK_STENCIL_OP_DECREMENT_AND_CLAMP };
	static const VkCompareOp depthfunc2vk[] = { VK_COMPARE_OP_LESS, VK_COMPARE_OP_LESS_OR_EQUAL, VK_COMPARE_OP_ALWAYS };
	static const VkBlendOp blendvk[] = { VK_BLEND_OP_ADD, VK_BLEND_OP_REVERSE_SUBTRACT };
	static const VkBlendFactor blendfactorvk[] = { VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA, VK_BLEND_FACTOR_SRC_ALPHA, VK_BLEND_FACTOR_ONE, VK_BLEND_FACTOR_ZERO };
	static const VkPolygonMode vkpolymode[] = { VK_POLYGON_MODE_FILL, VK_POLYGON_MODE_LINE };

	builder.setTopology(vktopology[(int)key.DrawType]);
	builder.setPolygonMode(vkpolymode[(int)key.Fill]);
	builder.setDepthStencilEnable(key.DepthTest, key.DepthWrite, key.StencilTest);
	builder.setDepthFunc(depthfunc2vk[key.DepthFunc]);
	builder.setDepthClampEnable(key.DepthClamp);
	builder.setDepthBias(key.DepthBias, 0.0f, 0.0f, 0.0f);

	// Note: CCW and CW is intentionally swapped here because the vulkan and opengl coordinate systems differ.
	// The vertex shader addresses this by patching up gl_Position.z, which has the side effect of flipping the sign of the front face calculations.
	builder.setCull(key.CullMode == Cull::None ? VK_CULL_MODE_NONE : VK_CULL_MODE_BACK_BIT, key.CullMode == Cull::Clockwise ? VK_FRONT_FACE_COUNTER_CLOCKWISE : VK_FRONT_FACE_CLOCKWISE);

	builder.setColorWriteMask((VkColorComponentFlags)key.ColorMask);
	builder.setStencil(VK_STENCIL_OP_KEEP, op2vk[key.StencilPassOp], VK_STENCIL_OP_KEEP, VK_COMPARE_OP_EQUAL, 0xffffffff, 0xffffffff, 0);

	bool blendEnable = !(key.BlendOp == BlendOperation::Add && key.SrcBlend == Blend::One && key.DestBlend == Blend::Zero);
	if (blendEnable)
		builder.setBlendMode(blendvk[(int)key.BlendOp], blendfactorvk[(int)key.SrcBlend], blendfactorvk[(int)key.DestBlend]);
	builder.setSubpassColorAttachmentCount(1);
	builder.setRasterizationSamples((VkSampleCountFlagBits)PassKey.Samples);

	builder.setLayout(fb->GetRenderPassManager()->GetPipelineLayout(key.NumTextureLayers));
	builder.setRenderPass(GetRenderPass(false, false, false));
	auto pipeline = builder.create(fb->Device.get());
	pipeline->SetDebugName("VkRenderPassSetup.Pipeline");
	return pipeline;
}
