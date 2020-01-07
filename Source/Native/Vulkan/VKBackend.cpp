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
#include "VKBackend.h"
#include "VKRenderDevice.h"
#include "VKVertexBuffer.h"
#include "VKIndexBuffer.h"
#include "VKTexture.h"

RenderDevice* VKBackend::NewRenderDevice(void* disp, void* window)
{
	try
	{
		return new VKRenderDevice(disp, window);
	}
	catch (const std::exception& e)
	{
		SetError("%s", e.what());
		return nullptr;
	}
}

void VKBackend::DeleteRenderDevice(RenderDevice* device)
{
	delete device;
}

VertexBuffer* VKBackend::NewVertexBuffer()
{
	return new VKVertexBuffer();
}

void VKBackend::DeleteVertexBuffer(VertexBuffer* buffer)
{
	VKRenderDevice::DeleteObject(static_cast<VKVertexBuffer*>(buffer));
}

IndexBuffer* VKBackend::NewIndexBuffer()
{
	return new VKIndexBuffer();
}

void VKBackend::DeleteIndexBuffer(IndexBuffer* buffer)
{
	VKRenderDevice::DeleteObject(static_cast<VKIndexBuffer*>(buffer));
}

Texture* VKBackend::NewTexture()
{
	return new VKTexture();
}

void VKBackend::DeleteTexture(Texture* texture)
{
	VKRenderDevice::DeleteObject(static_cast<VKTexture*>(texture));
}
