#pragma once
#include <opencv2/opencv.hpp>

using namespace cv;

Ptr<ml::SVM> svm;
const int img_area = 30 * 35;
const std::string charset = "BCDFGHJKMNPQRTVWXY2346789";

extern "C" __declspec(dllexport) void init(char* path);
extern "C" __declspec(dllexport) void release();
extern "C" __declspec(dllexport) char predict(float* data);