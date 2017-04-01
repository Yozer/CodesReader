#pragma once

struct CodeRect
{
	CodeRect(const int left, const int right, const int width, const int height)
		: left(left),
		  right(right),
		  width(width),
		  height(height)
	{
	}

	int left, right, width, height;
};

struct ArrayStruct
{
	CodeRect array[29];
	int length, width, height;
};