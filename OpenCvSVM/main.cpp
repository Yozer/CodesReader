#include <opencv2/opencv.hpp>
#include <ppl.h>
#include <experimental/filesystem>
#include <unordered_set>
namespace fs = std::experimental::filesystem;

using namespace cv;
const char* const traning_dir = "D:\\dataset\\grzego\\training_set";
const std::string charset = "BCDFGHJKMNPQRTVWXY2346789";
const int img_area = 30 * 35;
const int num_files = 49975;

void load_trening_data(Mat& traning_mat, Mat& labels);
void tranin();
void test_model(std::string name);

int main()
{
	tranin();
	//test_model("test_rbf_g10_c0.01_10e-6eps.yaml");
}

void test_model(std::string name)
{
	Ptr<ml::SVM> svm = ml::SVM::create();
	svm->load(name);


	for (const auto& p : fs::directory_iterator(traning_dir))
	{
		if (!fs::is_regular_file(p.status()))
			continue;


	}
}

void tranin()
{
	Mat training_mat(num_files, img_area, CV_32F);
	Mat labels(num_files, 1, CV_32S);

	load_trening_data(training_mat, labels);

	Ptr<ml::SVM> svm = ml::SVM::create();
	// edit: the params struct got removed,
	// we use setter/getter now:
	svm->setType(ml::SVM::C_SVC);
	svm->setKernel(ml::SVM::RBF);
	//svm->setDegree(3);
	//svm->setGamma(10);
	//svm->setC(0.01);
	//svm->setTermCriteria(Ter0mCriteria(TermCriteria::MAX_ITER, 100, 1e-6));
	svm->setTermCriteria(TermCriteria(TermCriteria::EPS | TermCriteria::COUNT, 1000, 10e-3));
	
	Ptr<ml::TrainData> td = ml::TrainData::create(training_mat, ml::ROW_SAMPLE, labels);
	//svm->train(td);
	svm->trainAuto(td);
	svm->save("test.yaml");
}

void load_trening_data(Mat& traning_mat, Mat& labels)
{
	int row_number = 0;

	std::vector<fs::path> files;
	files.reserve(num_files);
	int* const ptr_labels = labels.ptr<int>(0);

	for (fs::directory_entry p : fs::directory_iterator(traning_dir))
	{
		if (!fs::is_regular_file(p.status()))
			continue;

		files.push_back(p.path());
	}
	random_shuffle(files.begin(), files.end());

	std::mutex mutex;
	std::unordered_set<char> s;
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
			s.insert(letter);
			++row_number;
		}
	});
}
