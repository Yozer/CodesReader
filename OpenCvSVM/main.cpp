#include <opencv2/opencv.hpp>
#include <ppl.h>
#include <experimental/filesystem>
#include <unordered_set>
namespace fs = std::experimental::filesystem;

using namespace cv;
const char* const traning_dir = "C:\\grzego\\training_set";
const char* const validation_set = "C:\\grzego\\validation_set";
const std::string charset = "BCDFGHJKMNPQRTVWXY2346789";
const int img_area = 15 * 25;

void load_data(Mat& traning_mat, Mat& labels, const char* path);
void tranin(const char* dir);
void validate_model(std::string name, const char* dir);

int main()
{
	tranin(traning_dir);
	validate_model("test.yaml", validation_set);
	validate_model("test.yaml", traning_dir);
}

void validate_model(std::string name, const char* dir)
{
	Ptr<ml::SVM> svm = Algorithm::load<ml::SVM>(name);

	Mat input, labels, predictedLabels;

	load_data(input, labels, dir);
	svm->predict(input, predictedLabels);

	if (!predictedLabels.isContinuous())
		throw std::exception();

	const int* const ptr_labels = labels.ptr<int>(0);
	const float* const ptr_predicted = predictedLabels.ptr<float>(0);

	int failed = 0;
	for (int i = 0; i < labels.rows; ++i)
	{
		if (ptr_labels[i] != ((int)ptr_predicted[i] + 0.5))
			++failed;
	}

	std::cout << failed << '/' << labels.rows << " succ: " << (float)failed / labels.rows << std::endl;
}

void tranin(const char* dir)
{
	Mat training_mat, labels;

	load_data(training_mat, labels, dir);

	Ptr<ml::SVM> svm = ml::SVM::create();

	svm->setType(ml::SVM::C_SVC);
	svm->setKernel(ml::SVM::RBF);
	//svm->setDegree(3);
	svm->setCoef0(0.05);
	svm->setGamma(0.1);
	//svm->setC(0.5);
	//svm->setTermCriteria(Ter0mCriteria(TermCriteria::MAX_ITER, 100, 1e-6));
	svm->setTermCriteria(TermCriteria(TermCriteria::EPS | TermCriteria::EPS , 10000, 10e-4));

	Ptr<ml::TrainData> td = ml::TrainData::create(training_mat, ml::ROW_SAMPLE, labels);
	svm->train(td);
	auto emptyGrid = ml::ParamGrid(0, 0, 1);
	//svm->trainAuto(td, 10, ml::SVM::getDefaultGrid(ml::SVM::C), ml::SVM::getDefaultGrid(ml::SVM::GAMMA), 
	//	emptyGrid, emptyGrid, emptyGrid, emptyGrid);
	//svm->trainAuto(td, 10, emptyGrid, ml::SVM::getDefaultGrid(ml::SVM::GAMMA), emptyGrid, emptyGrid,
	//	ml::SVM::getDefaultGrid(ml::SVM::COEF), ml::SVM::getDefaultGrid(ml::SVM::DEGREE));
	svm->save("test2.yaml");
}

void load_data(Mat& traning_mat, Mat& labels, const char* path)
{
	int row_number = 0;
	std::vector<fs::path> files;

	for (fs::directory_entry p : fs::directory_iterator(path))
	{
		if (!is_regular_file(p.status()))
			continue;

		files.push_back(p.path());
	}

	std::random_shuffle(files.begin(), files.end());
	//files = std::vector<fs::path>(files.begin(), files.begin() + 10000);

	traning_mat.create(static_cast<int>(files.size()), img_area, CV_32F);
	labels.create(static_cast<int>(files.size()), 1, CV_32S);

	if (!traning_mat.isContinuous() || !labels.isContinuous())
		throw std::exception();

	int* const ptr_labels = labels.ptr<int>(0);
	std::mutex mutex;
	concurrency::parallel_for(static_cast<size_t>(0), files.size(), [&](size_t index)
	{
		const auto& p = files[index];

		Mat img = imread(p.string(), CV_LOAD_IMAGE_UNCHANGED);
		img.reshape(0, 1).convertTo(img, CV_32F);
		img = img / 255.0f;

		const auto file_name = p.filename().string();
		const auto letter = file_name.substr(0, file_name.find('_'))[0];

		{
			std::lock_guard<std::mutex> lock(mutex);
			img.row(0).copyTo(traning_mat.row(row_number));
			ptr_labels[row_number] = static_cast<int>(charset.find(letter));
			++row_number;
		}
	});
}
