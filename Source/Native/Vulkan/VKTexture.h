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

#include "../Backend.h"
#include <list>

class VKRenderDevice;
class VkTextureImage;

class VKTexture : public Texture
{
public:
	VKTexture();
	~VKTexture();

	void Finalize();

	void Set2DImage(int width, int height) override;
	void SetCubeImage(int size) override;

	bool IsCubeTexture() const { return mCubeTexture; }
	int GetWidth() const { return mWidth; }
	int GetHeight() const { return mHeight; }

	VKRenderDevice* Device = nullptr;
	std::list<VKTexture*>::iterator ItTexture;

	std::unique_ptr<VkTextureImage> Image;
	std::unique_ptr<VkTextureImage> DepthStencil;

private:
	int mWidth = 0;
	int mHeight = 0;
	bool mCubeTexture = false;
	bool mPBOTexture = false;
};
