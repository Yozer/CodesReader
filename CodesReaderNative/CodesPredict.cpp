#include "CodesPredict.h"
using namespace cv;

extern "C" __declspec(dllexport) void init(char* path)
{
	svm = ml::SVM::load(path);
}

extern "C" __declspec(dllexport) void release()
{
	svm.release();
}

extern "C" __declspec(dllexport) void predict(float* data, int count, char16_t* result)
{
	Mat input(count, img_area, CV_32F, data);
	input /= 255.0f;
	Mat predicted;

	svm->predict(input, predicted);
	const float* const ptr = predicted.ptr<float>(0);

	for(int i = 0; i < count; ++i)
	{
		result[i] = charset[(int)(ptr[i] + 0.5)];
	}
}

