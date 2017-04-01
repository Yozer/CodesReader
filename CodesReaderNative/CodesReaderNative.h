// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the CODESREADERNATIVE_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// CODESREADERNATIVE_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef CODESREADERNATIVE_EXPORTS
#define CODESREADERNATIVE_API __declspec(dllexport)
#else
#define CODESREADERNATIVE_API __declspec(dllimport)
#endif

#include <opencv2/opencv.hpp>
using namespace cv;

struct CodeRect
{
	CodeRect(const int left, const int top, const int width, const int height)
		: left(left),
		top(top),
		width(width),
		height(height)
	{
	}

	int left, top, width, height;
};

struct ArrayStruct
{
	CodeRect array[29];
	int length;
};

const int LEFT_OFFSET = -49;

void PreProcess(Mat& source, Mat& buffer);
Rect Process(Mat& source, Mat& buffer, double threshold);
void RemoveSmallObjects(Mat& src);
void ClearBorder(Mat& img);
Rect FindBiggestBlob(const Mat& img);
uchar* MatToBytes(Mat& image, ArrayStruct& result);
Rect TryProcess(Mat image, Mat buffer, double threshold);
extern "C" __declspec(dllexport) void segment_codes(char* path, ArrayStruct& result, unsigned char*& codePtr);
