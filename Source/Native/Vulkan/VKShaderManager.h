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

#include "VKRenderDevice.h"

class VkShaderProgram
{
public:
	std::unique_ptr<VulkanShader> vert;
	std::unique_ptr<VulkanShader> frag;
};

class VkShaderManager
{
public:
	VkShaderManager(VKRenderDevice* fb);

	void DeclareShader(ShaderName index, const char* name, const char* vertexshader, const char* fragmentshader);
	VkShaderProgram* Get(ShaderName index, bool alphatest);

private:
	VKRenderDevice* fb = nullptr;
	std::map<ShaderName, std::unique_ptr<VkShaderProgram>> mShaders;
	std::map<ShaderName, std::unique_ptr<VkShaderProgram>> mShadersAlphaTest;
};
