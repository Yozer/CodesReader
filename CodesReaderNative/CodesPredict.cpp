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

char predict(float* data)
{
	//Mat img = imread("C:\\input_letters\\B_1889.bmp", CV_LOAD_IMAGE_UNCHANGED);
	//img.reshape(0, 1).convertTo(img, CV_32F);

	Mat mat(1, img_area, CV_32F, data);
	mat /= 255.0f;
	int predicted = svm->predict(mat) + 0.5f;
	return charset[predicted];
}

