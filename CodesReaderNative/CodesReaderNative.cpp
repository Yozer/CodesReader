// CodesReaderNative.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "CodesReaderNative.h"
#include <Objbase.h>
#include <vector>

extern "C" __declspec(dllexport) void segment_codes(char* path, ArrayStruct& result, uchar*& codePtr)
{
	cuda::GpuMat buffer, source;
	Mat img = imread(path, CV_LOAD_IMAGE_GRAYSCALE);
	source.upload(img);

	PreProcess(source, buffer);
	source.download(img);
	Rect codeRect = Process(source, buffer);

	if(codeRect.width > 450 | codeRect.width < 400 || codeRect.height > 28 || codeRect.height < 13)
	{
		codePtr = nullptr;
	}
	else
	{
		if(codeRect.width + codeRect.x > img.cols)
		{
			throw std::exception();
		}
		if (codeRect.height + codeRect.y > img.rows)
		{
			throw std::exception();
		}
		if (codeRect.y < 0 || codeRect.x < 0)
		{
			throw std::exception();
		}
		Mat subRect = img(codeRect);
		codePtr = MatToBytes(subRect, result);
	}
}

uchar* MatToBytes(Mat& image, ArrayStruct& result)
{
	std::vector<uchar> buff;
	imencode(".bmp", image, buff);
	auto codePtr = (uchar*)CoTaskMemAlloc(buff.size());
	memcpy(codePtr, &buff[0], buff.size());
	result.length = buff.size();

	return codePtr;
}

void inline Inverse(cuda::GpuMat& source, cuda::GpuMat& buffer)
{
	cuda::bitwise_not(source, buffer);
	std::swap(source, buffer);
}

Rect Process(cuda::GpuMat& source, cuda::GpuMat& buffer)
{
	cuda::threshold(source, buffer, 204.0, 255.0, CV_THRESH_BINARY_INV);
	std::swap(source, buffer);

	RemoveSmallObjects(source);
	Inverse(source, buffer);

	auto strel = getStructuringElement(MORPH_RECT, Size(45, 1), Point(0, 0));
	auto horizontalOpening = cuda::createMorphologyFilter(MORPH_OPEN, CV_8UC1, strel);

	horizontalOpening->apply(source, buffer);
	std::swap(source, buffer);
	Inverse(source, buffer);

	// back to CPU
	Mat img, buff;
	source.download(img);
	ClearBorder(img);

	strel = getStructuringElement(MORPH_RECT, Size(1, 5), Point(0, 0));
	morphologyEx(img, buff, MORPH_OPEN, strel);

	return FindBiggestBlob(buff);
}

void PreProcess(cuda::GpuMat& source, cuda::GpuMat& buffer)
{
	auto size = source.size();
	int width = std::max(size.width, size.height) - 99;
	int height = std::min(size.width, size.height);

	double rotate = 0, yShift = 0;
	if (size.height > size.width)
	{
		rotate = 90.0;
		yShift = height;
	}

	cuda::rotate(source, buffer, Size2i(width, height), rotate, LEFT_OFFSET, yShift);
	std::swap(source, buffer);
}

Rect FindBiggestBlob(const Mat& img)
{
	Mat labels, stats, centroids;
	const int number = connectedComponentsWithStats(img, labels, stats, centroids);

	if (!img.isContinuous() || !labels.isContinuous() || !stats.isContinuous())
		std::cout << "WTF";

	const uchar* const ptr = img.ptr<uchar>(0);
	const int* const statsPtr = stats.ptr<int>(0);
	int maxArea = 0;
	int labelIndex = 0;

	for (int i = 1; i < number; i++)
	{
		const int area = statsPtr[i * CC_STAT_MAX + CC_STAT_AREA];
		if (area > maxArea)
		{
			labelIndex = i;
			maxArea = area;
		}
	}

	const int left = statsPtr[labelIndex * CC_STAT_MAX + CC_STAT_LEFT];
	int width = statsPtr[labelIndex * CC_STAT_MAX + CC_STAT_WIDTH];
	const int top = statsPtr[labelIndex * CC_STAT_MAX + CC_STAT_TOP];
	int height = statsPtr[labelIndex * CC_STAT_MAX + CC_STAT_HEIGHT];

	//if (left + width > img.cols)
	//	--width;
	//if (top + height > img.rows)
	//	--height;

	return Rect(left , top, width, height);
}

void ClearBorder(Mat& img)
{
	Mat labels, stats, centroids;
	const int number = connectedComponentsWithStats(img, labels, stats, centroids);

	if (!img.isContinuous() || !labels.isContinuous() || !stats.isContinuous())
		std::cout << "WTF";

	uchar* const ptr = img.ptr<uchar>(0);
	const int* const labelsPtr = labels.ptr<int>(0);
	const int* const statsPtr = stats.ptr<int>(0);

	for (int i = 1; i < number; i++)
	{
		const int left = statsPtr[i * CC_STAT_MAX + CC_STAT_LEFT];
		const int right = statsPtr[i * CC_STAT_MAX + CC_STAT_WIDTH] + left;
		const int top = statsPtr[i * CC_STAT_MAX + CC_STAT_TOP];
		const int bottom = statsPtr[i * CC_STAT_MAX + CC_STAT_HEIGHT] + top;

		if (left == 0 || right == img.cols || top == 0)
		{
			for (int x = left; x < right; x++)
			{
				for (int y = top; y < bottom; y++)
				{
					if (labelsPtr[labels.cols * y + x] == i)
					{
						ptr[img.cols * y + x] = 0;
					}
				}
			}
		}
	}
}

void RemoveSmallObjects(cuda::GpuMat& src)
{
	const int MAX_AREA = 8;
	Mat img, labels, stats, centroids;
	src.download(img);

	const int number = connectedComponentsWithStats(img, labels, stats, centroids);

	if (!img.isContinuous() || !labels.isContinuous() || !stats.isContinuous())
		std::cout << "WTF";

	uchar* const ptr = img.ptr<uchar>(0);
	const int* const labelsPtr = labels.ptr<int>(0);
	const int* const statsPtr = stats.ptr<int>(0);

	for (int i = 1; i < number; i++)
	{
		const int area = statsPtr[i * CC_STAT_MAX + CC_STAT_AREA];
		if (area < MAX_AREA)
		{
			const int left = statsPtr[i * CC_STAT_MAX + CC_STAT_LEFT];
			const int right = statsPtr[i * CC_STAT_MAX + CC_STAT_WIDTH] + left;
			const int top = statsPtr[i * CC_STAT_MAX + CC_STAT_TOP];
			const int bottom = statsPtr[i * CC_STAT_MAX + CC_STAT_HEIGHT] + top;

			for (int x = left; x < right; x++)
			{
				for (int y = top; y < bottom; y++)
				{
					if (labelsPtr[labels.cols * y + x] == i)
					{
						ptr[img.cols * y + x] = 0;
					}
				}
			}
		}
	}

	src.upload(img);
}