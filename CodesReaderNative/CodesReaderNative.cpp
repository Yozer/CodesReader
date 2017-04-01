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

bool FillWithCodes(const Mat& source, ArrayStruct& result, double thr, int strelWidth)
{
	Mat buff, img;
	threshold(source, img, thr, 255.0, CV_THRESH_BINARY);
	auto strel = getStructuringElement(MORPH_RECT, Size(strelWidth, 45), Point(0, 0));
	morphologyEx(img, buff, MORPH_OPEN, strel);
	bitwise_not(buff, img);

	Mat labels, stats, centroids;
	const int number = connectedComponentsWithStats(img, labels, stats, centroids);

	if (number != 30) // background counts as one
		return false;

	if (!img.isContinuous() || !labels.isContinuous() || !stats.isContinuous())
		throw std::exception();

	const int* const statsPtr = stats.ptr<int>(0);

	for(int i = 1; i < number; ++i)
	{
		const int left = statsPtr[i * CC_STAT_MAX + CC_STAT_LEFT];
		const int width = statsPtr[i * CC_STAT_MAX + CC_STAT_WIDTH];
		const int top = statsPtr[i * CC_STAT_MAX + CC_STAT_TOP];
		const int height = statsPtr[i * CC_STAT_MAX + CC_STAT_HEIGHT];

		result.array[i - 1] = CodeRect(left, top, width, height);
	}

	return true;
}

bool SplitCodes(const Mat& source, ArrayStruct& result)
{
	bool res = FillWithCodes(source, result, 204.0, 1);
	if (!res)
		res = FillWithCodes(source, result, 153.0, 1);
	if (!res)
		res = FillWithCodes(source, result, 204.0, 2);
	if (!res)
		res = FillWithCodes(source, result, 153.0, 2);

	return res;
}
//bool SplitCodes(const Mat& source, ArrayStruct& result)
//{
//	Mat img;
//	threshold(source, img, 150.0, 255.0, CV_THRESH_BINARY);
//	bool isBlackPixel = false;
//	int prev_x = 0;
//	int index = 0;
//
//	for (int x = 3; x < img.cols; x++)
//	{
//		if (!isBlackPixel)
//		{
//			for (int y = 0; y < img.rows; y++)
//			{
//				if (img.at<uchar>(y, x) == 0)
//				{
//					isBlackPixel = true;
//					result.array[index++] = CodeRect(prev_x, 0, x - 1 - prev_x, img.rows);
//					prev_x = x - 1;
//					break;
//				}
//			}
//		}
//
//		if (isBlackPixel)
//		{
//			bool allWhite = true;
//			for (int y = 0; y < img.rows; y++)
//			{
//				if (img.at<uchar>(y, x) == 0)
//				{
//					allWhite = false;
//					break;
//				}
//			}
//
//			if (allWhite)
//			{
//				isBlackPixel = false;
//			}
//		}
//	}
//
//	return index == 29;
//}

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

	RemoveSmallObjects(source);
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

void RemoveSmallObjects(Mat& src)
{
	const int MAX_AREA = 8;
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
						ptr[src.cols * y + x] = 0;
					}
				}
			}
		}
	}
}