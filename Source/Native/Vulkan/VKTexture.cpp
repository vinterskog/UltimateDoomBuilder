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
#include "VKTexture.h"
#include "VKRenderDevice.h"
#include <stdexcept>

VKTexture::VKTexture()
{
}

VKTexture::~VKTexture()
{
	Finalize();
}

void VKTexture::Finalize()
{
	if (Device)
	{
		Device->mTextures.erase(ItTexture);
		Device = nullptr;
	}
}

void VKTexture::Set2DImage(int width, int height)
{
	if (width < 1) width = 1;
	if (height < 1) height = 1;
	mCubeTexture = false;
	mWidth = width;
	mHeight = height;
}

void VKTexture::SetCubeImage(int size)
{
	mCubeTexture = true;
	mWidth = size;
	mHeight = size;
}

bool VKTexture::SetPixels(VKRenderDevice* device, const void* data)
{
	return true;
}

bool VKTexture::SetCubePixels(VKRenderDevice* device, CubeMapFace face, const void* data)
{
	return true;
}
