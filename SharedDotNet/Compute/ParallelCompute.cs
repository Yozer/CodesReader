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
        private const int ThreadCount = 8;
        private readonly IImageProcessor _imageProcessor;
        private readonly IClassifier _classifier;
        private BlockingCollection<ComputeResult> _queue;

        public ParallelCompute(IImageProcessor imageProcessor, IClassifier classifier)
        {
            _imageProcessor = imageProcessor;
            _classifier = classifier;
        }

        public IEnumerable<ComputeResult> Compute(IEnumerable<string> imagesPath)
        {
            _queue = new BlockingCollection<ComputeResult>();
            Thread thread = CreateReaderThread(imagesPath);

            //var timeout = TimeSpan.FromMilliseconds(10);
            const int bufferSize = 500;
            var buffer = new List<ComputeResult>(bufferSize); // 1000 codes
            //var results = new List<ComputeResult>();

            while (_queue.TryTake(out ComputeResult item, Timeout.Infinite))
            {
                // do something with item
                buffer.Add(item);
                if (buffer.Count == bufferSize)
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
            //return results;
        }

        private IEnumerable<ComputeResult> ComputeResults(List<ComputeResult> buffer)
        {
            _classifier.Recognize(buffer.Where(t => t.Letters != null).ToList());
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
                    ComputeResult result = _imageProcessor.SegmentCode(file);
                    _queue.Add(result);
                });

                _queue.CompleteAdding();
            });

            thread.Start();
            return thread;
        }

        public void Dispose()
        {
            _classifier.Dispose();
            _imageProcessor.Dispose();
        }
    }
}