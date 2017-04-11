// CodesReaderNative.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "CodesReaderNative.h"
#include <Objbase.h>
#include <vector>

extern "C" __declspec(dllexport) void segment_codes(char* path, ArrayStruct& result, uchar*& codePtr)
{
	Mat img = imread(path, CV_LOAD_IMAGE_GRAYSCALE);
	Mat buffer;
	PreProcess(img, buffer);

	Rect codeRect = TryProcess(img.clone(), buffer, 204.0);
	if(codeRect.width == 0)
		codeRect = TryProcess(img.clone(), buffer, 153.0);

	if(codeRect.width == 0)
	{
		codePtr = nullptr;
	}
	else
	{
		Mat subRect = img(codeRect);
		SplitCodes(subRect, result);
		codePtr = MatToBytes(subRect, result);
	}
}

bool FindCodes(const Mat& source, ArrayStruct& result, double thr, int strelWidth, bool adaptive = false)
{
	const int minLetterWidth = 3;
	Mat buff, img;

	if(adaptive)
		adaptiveThreshold(source, img, 255.0, ADAPTIVE_THRESH_GAUSSIAN_C, THRESH_BINARY, thr, 5);
	else
		threshold(source, img, thr, 255.0, CV_THRESH_BINARY);

//#if _DEBUG
//	imwrite("D:\\test.bmp", img);
//#endif 
	bitwise_not(img, buff);
	RemoveSmallObjects(buff, 8);
	bitwise_not(buff, img);
//#if _DEBUG
//	imwrite("D:\\test.bmp", img);
//#endif 

	uchar* const ptrImage = img.ptr(0);
	const int upper = 2.0 / 5 * source.rows + 0.5; // (source.rows / 2.0 + 0.5) - 1;
	for(int i = upper; i > 0; --i)
	{
		for(int x = 0; x < source.cols; ++x)
		{
			if (ptrImage[i * source.cols + x] == 0) // black
			{
				ptrImage[(i - 1) * source.cols + x] = 0;
			}
		}
	}

	for (int i = source.rows - upper - 1; i < source.rows - 1; ++i)
	{
		for (int x = 0; x < source.cols; ++x)
		{
			if (ptrImage[i * source.cols + x] == 0) // black
			{
				ptrImage[(i + 1) * source.cols + x] = 0;
			}
		}
	}

//#if _DEBUG
//	imwrite("D:\\test.bmp", img);
//#endif 
	auto strel = getStructuringElement(MORPH_RECT, Size(strelWidth, 7), Point(0, 0));
	morphologyEx(img, buff, MORPH_ERODE, strel);
//#if _DEBUG
//	imwrite("D:\\test.bmp", buff);
//#endif


	bitwise_not(buff, img);
	//std::swap(img, buff);
	Mat labels, stats, centroids;
	const int number = connectedComponentsWithStats(img, labels, stats, centroids);


	if (!img.isContinuous() || !labels.isContinuous() || !stats.isContinuous())
		throw std::exception();

	const int* const statsPtr = stats.ptr<int>(0);
	int index = 0;

	for(int i = 1; i < number; ++i)
	{
		const int left = statsPtr[i * CC_STAT_MAX + CC_STAT_LEFT];
		const int width = statsPtr[i * CC_STAT_MAX + CC_STAT_WIDTH];
		const int top = statsPtr[i * CC_STAT_MAX + CC_STAT_TOP];
		const int height = statsPtr[i * CC_STAT_MAX + CC_STAT_HEIGHT];

		if (width < minLetterWidth ||
			top != 0 || height != source.rows) // skip dashes and other objects than letters
		{
			continue;
		}

		if(index < 25)
			result.array[index] = CodeRect(left, 0, width, source.rows);
		index++;
	}

	if (index != 25)
		return false;

	return true;
}

bool SplitCodes(const Mat& source, ArrayStruct& result)
{
	bool res = FindCodes(source, result, 204.0, 1);
	if (!res)
		res = FindCodes(source, result, 153.0, 1);
	if (!res)
		res = FindCodes(source, result, 204.0, 2);
	if (!res)
		res = FindCodes(source, result, 153.0, 2);
	if (!res)
		res = FindCodes(source, result, 11, 1, true);
	if (!res)
		res = FindCodes(source, result, 11, 2, true);
	
	return res;
}

Rect TryProcess(Mat image, Mat buffer, double threshold)
{
	Rect codeRect = Process(image, buffer, threshold);

	if (codeRect.width > 450 | codeRect.width < 400 || codeRect.height > 28 || codeRect.height < 13)
	{
		return Rect();
	}
	
	return codeRect;
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

void inline Inverse(Mat& source, Mat& buffer)
{
	bitwise_not(source, buffer);
	std::swap(source, buffer);
}

Rect Process(Mat& source, Mat& buffer, double thr)
{
	threshold(source, buffer, thr, 255.0, CV_THRESH_BINARY_INV);
	std::swap(source, buffer);

	RemoveSmallObjects(source, 8);
	Inverse(source, buffer);

	auto strel = getStructuringElement(MORPH_RECT, Size(45, 1), Point(0, 0));
	morphologyEx(source, buffer, MORPH_OPEN, strel);
	std::swap(source, buffer);

	Inverse(source, buffer);
	ClearBorder(source);
	Inverse(source, buffer);

	strel = getStructuringElement(MORPH_RECT, Size(1, 5), Point(0, 0));
	morphologyEx(source, buffer, MORPH_OPEN, strel);
	std::swap(source, buffer);

	Inverse(source, buffer);
	return FindBiggestBlob(source);
}

void PreProcess(Mat& source, Mat& buffer)
{
	auto size = source.size();
	int width = std::max(size.width, size.height) - 99;
	int height = std::min(size.width, size.height);

	if (size.height > size.width)
	{
		rotate(source, buffer, ROTATE_90_COUNTERCLOCKWISE);
		std::swap(source, buffer);
		buffer.release();
		buffer = Mat();
	}

	Rect myROI(-LEFT_OFFSET, 0, width, height);
	source = source(myROI);
}

Rect FindBiggestBlob(const Mat& img)
{
	Mat labels, stats, centroids;
	const int number = connectedComponentsWithStats(img, labels, stats, centroids);

	if (!img.isContinuous() || !labels.isContinuous() || !stats.isContinuous())
		throw std::exception();

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
	const int width = statsPtr[labelIndex * CC_STAT_MAX + CC_STAT_WIDTH];
	const int top = statsPtr[labelIndex * CC_STAT_MAX + CC_STAT_TOP];
	const int height = statsPtr[labelIndex * CC_STAT_MAX + CC_STAT_HEIGHT];

	return Rect(left , top, width, height);
}

void ClearBorder(Mat& img)
{
	Mat labels, stats, centroids;
	const int number = connectedComponentsWithStats(img, labels, stats, centroids);

	if (!img.isContinuous() || !labels.isContinuous() || !stats.isContinuous())
		throw std::exception();

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

void RemoveSmallObjects(Mat& src, const int minArea)
{
	Mat labels, stats, centroids;

	const int number = connectedComponentsWithStats(src, labels, stats, centroids);

	if (!src.isContinuous() || !labels.isContinuous() || !stats.isContinuous())
		throw std::exception();

	uchar* const ptr = src.ptr<uchar>(0);
	const int* const labelsPtr = labels.ptr<int>(0);
	const int* const statsPtr = stats.ptr<int>(0);

	for (int i = 1; i < number; i++)
	{
		const int area = statsPtr[i * CC_STAT_MAX + CC_STAT_AREA];
		if (area < minArea)
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
						ptr[src.cols * y + x] = 0;
					}
				}
			}
		}
	}
}