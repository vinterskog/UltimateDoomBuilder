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
#include "VKShaderManager.h"
#include "System/VulkanBuilders.h"

VkShaderManager::VkShaderManager(VKRenderDevice* fb) : fb(fb)
{
}

void VkShaderManager::DeclareShader(ShaderName index, const char* name, const char* vertexshader, const char* fragmentshader)
{
	std::string prefixNAT = R"(
		#version 450
		#define VULKAN
		#line 1
	)";
	std::string prefixAT = R"(
		#version 450
		#define VULKAN
		#define ALPHA_TEST
		#line 1
	)";

	mShaders[index] = CreateProgram(prefixNAT + vertexshader, prefixNAT + fragmentshader);
	mShadersAlphaTest[index] = CreateProgram(prefixAT + vertexshader, prefixAT + fragmentshader);
}

std::unique_ptr<VkShaderProgram> VkShaderManager::CreateProgram(const std::string& vertexsrc, const std::string& fragmentsrc)
{
	std::unique_ptr<VkShaderProgram> program(new VkShaderProgram());

	ShaderBuilder vertbuilder;
	vertbuilder.setVertexShader(vertexsrc);
	program->vert = vertbuilder.create(fb->Device.get());

	ShaderBuilder fragbuilder;
	fragbuilder.setFragmentShader(fragmentsrc);
	program->frag = fragbuilder.create(fb->Device.get());

	return program;
}

VkShaderProgram* VkShaderManager::Get(ShaderName index, bool alphatest)
{
	return alphatest ? mShadersAlphaTest[index].get() : mShaders[index].get();
}
