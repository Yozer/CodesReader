using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharedDotNet.Classifier;
using SharedDotNet.Imaging;

namespace SharedDotNet.Compute
{
    public class ParallelCompute : ICompute
    {
        private const int ThreadCount = 7;
        public IImageProcessor ImageProcessor { get; }
        public IClassifier Classifier { get; }
        private BlockingCollection<ComputeResult> _queue;

        public ParallelCompute(IImageProcessor imageProcessor, IClassifier classifier)
        {
            ImageProcessor = imageProcessor;
            Classifier = classifier;
        }

        public IEnumerable<ComputeResult> Compute(IEnumerable<string> imagesPath)
        {
            _queue = new BlockingCollection<ComputeResult>();
            Thread thread = CreateReaderThread(imagesPath);

            var buffer = new List<ComputeResult>(Classifier.BufferSize); // 1000 codes

            while (_queue.TryTake(out ComputeResult item, Timeout.Infinite))
            {
                // do something with item
                buffer.Add(item);
                if (buffer.Count == Classifier.BufferSize)
                {
                    foreach (var computeResult in ComputeResults(buffer))
                        yield return computeResult;
                }
            }

            if (buffer.Count > 0)
            {
                foreach (var computeResult in ComputeResults(buffer))
                    yield return computeResult;
            }

            _queue.Dispose();
        }

        private IEnumerable<ComputeResult> ComputeResults(List<ComputeResult> buffer)
        {
            Classifier.Recognize(buffer.Where(t => t.Letters != null).ToList());
            foreach (var result in buffer)
                yield return result;
            buffer.Clear();
        }

        private Thread CreateReaderThread(IEnumerable<string> imagesPath)
        {
            var thread = new Thread(() =>
            {
                Parallel.ForEach(imagesPath, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount }, file =>
                {
                    ComputeResult result = ImageProcessor.SegmentCode(file);
                    _queue.Add(result);
                });

                _queue.CompleteAdding();
            });

            thread.Start();
            return thread;
        }

        public void Dispose()
        {
            Classifier.Dispose();
            ImageProcessor.Dispose();
        }
    }
}