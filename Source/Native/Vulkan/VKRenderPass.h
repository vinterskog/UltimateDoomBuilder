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

#pragma once

#include "System/VulkanObjects.h"
#include "../Backend.h"

class VKRenderDevice;
enum class VertexFormat;

class VkPipelineKey
{
public:
	ShaderName Shader;
	int AlphaTest;
	int DepthWrite;
	int DepthTest;
	int DepthFunc;
	int DepthClamp;
	int DepthBias;
	int StencilTest;
	int StencilPassOp;
	int ColorMask;
	Cull CullMode;
	BlendOperation BlendOp;
	Blend SrcBlend;
	Blend DestBlend;
	FillMode Fill;
	int VertexFormat;
	PrimitiveType DrawType;
	int NumTextureLayers;

	bool operator<(const VkPipelineKey& other) const { return memcmp(this, &other, sizeof(VkPipelineKey)) < 0; }
	bool operator==(const VkPipelineKey& other) const { return memcmp(this, &other, sizeof(VkPipelineKey)) == 0; }
	bool operator!=(const VkPipelineKey& other) const { return memcmp(this, &other, sizeof(VkPipelineKey)) != 0; }
};

class VkRenderPassKey
{
public:
	int DepthStencil;
	int Samples;
	VkFormat DrawBufferFormat;
	VkFormat DepthStencilFormat;

	bool operator<(const VkRenderPassKey& other) const { return memcmp(this, &other, sizeof(VkRenderPassKey)) < 0; }
	bool operator==(const VkRenderPassKey& other) const { return memcmp(this, &other, sizeof(VkRenderPassKey)) == 0; }
	bool operator!=(const VkRenderPassKey& other) const { return memcmp(this, &other, sizeof(VkRenderPassKey)) != 0; }
};

class VkRenderPassSetup
{
public:
	VkRenderPassSetup(VKRenderDevice* fb, const VkRenderPassKey& key);

	VulkanRenderPass* GetRenderPass(bool clearColor, bool clearDepth, bool clearStencil);
	VulkanPipeline* GetPipeline(const VkPipelineKey& key);

	VkRenderPassKey PassKey;
	std::unique_ptr<VulkanRenderPass> RenderPasses[8];
	std::map<VkPipelineKey, std::unique_ptr<VulkanPipeline>> Pipelines;

private:
	std::unique_ptr<VulkanRenderPass> CreateRenderPass(bool clearColor, bool clearDepth, bool clearStencil);
	std::unique_ptr<VulkanPipeline> CreatePipeline(const VkPipelineKey& key);

	VKRenderDevice* fb;
};

class VertexBufferAttribute
{
public:
	int location = 0;
	int binding = 0;
	VkFormat format = VK_FORMAT_R32G32B32A32_SFLOAT;
	size_t offset = 0;
};

class VkVertexFormat
{
public:
	int NumBindingPoints;
	size_t Stride;
	std::vector<VertexBufferAttribute> Attrs;
};

class VkRenderPassManager
{
public:
	VkRenderPassManager(VKRenderDevice *fb);
	~VkRenderPassManager();

	void Init();
	void RenderBuffersReset();
	void UpdateUniformSet();
	void TextureSetPoolReset();

	VkRenderPassSetup* GetRenderPass(const VkRenderPassKey& key);
	int GetVertexFormat(int numBindingPoints, int numAttributes, size_t stride, const VertexBufferAttribute* attrs);

	VkVertexFormat* GetVertexFormat(int index);

	std::unique_ptr<VulkanDescriptorSet> AllocateTextureDescriptorSet(int numLayers);
	VulkanPipelineLayout* GetPipelineLayout(int numLayers);

	std::unique_ptr<VulkanDescriptorSetLayout> UniformSetLayout;
	std::map<VkRenderPassKey, std::unique_ptr<VkRenderPassSetup>> RenderPassSetup;

	std::unique_ptr<VulkanDescriptorSet> UniformSet;

private:
	void CreateUniformSetLayout();
	void CreateDescriptorPool();
	void CreateUniformSet();

	VulkanDescriptorSetLayout* GetTextureSetLayout(int numLayers);

	VKRenderDevice* fb = nullptr;
	int TextureDescriptorSetsLeft = 0;
	int TextureDescriptorsLeft = 0;
	std::vector<std::unique_ptr<VulkanDescriptorPool>> TextureDescriptorPools;
	std::unique_ptr<VulkanDescriptorPool> UniformDescriptorPool;
	std::vector<std::unique_ptr<VulkanDescriptorSetLayout>> TextureSetLayouts;
	std::vector<std::unique_ptr<VulkanPipelineLayout>> PipelineLayouts;
	std::vector<VkVertexFormat> VertexFormats;
};
