using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using R3;



namespace TestR
{
    internal class Program
    {
       private static  int Main(string[] args)
        {
            //TaskCompletionSource tcs = new TaskCompletionSource();

            //// Поток 1: выполняет асинхронную операцию и сообщает завершение через TaskCompletionSource
            //_ = Task.Run(async () =>
            //{
            //    await Task.Delay(2000);  // Симуляция работы
            //    Console.WriteLine("Операция завершена.");
            //    tcs.SetResult();  // Сигнализируем о завершении операции
            //});

            //// Поток 2: ожидает завершения операции
            //Console.WriteLine("Ожидание завершения операции...");

            //Console.WriteLine("Операция завершена, продолжение работы.");

            //TaskCompletionSource tcs = new TaskCompletionSource();
            //R3.BehaviorSubject<bool> behaviorSubject = new BehaviorSubject<bool>(false);

            //IDisposable? d = null;

            //d = behaviorSubject.SubscribeOn(synchronizationContext:null).ObserveOn(synchronizationContext: null).Subscribe((nextValue) =>
            //{

            //    if(nextValue is true)
            //    {
            //        d?.Dispose();
            //        tcs.SetResult();
            //    }

            //});
            //behaviorSubject.OnNext(true);
            //tcs.Task.Wait(); 
            //R3.ReactiveCommand reactiveCommand = new R3.ReactiveCommand();


            R3.BehaviorSubject<bool> behaviorSubject = new BehaviorSubject<bool>(false);

            Task testR = Task.Run(() =>
            {

                TaskCompletionSource tcs = new TaskCompletionSource();
                IDisposable? d = null;

                d = behaviorSubject.SubscribeOn(synchronizationContext: null).ObserveOn(synchronizationContext: null).
                    SubscribeAwait(async (x, _) =>
                    {
                        if(x is true)
                        {
                            await Task.Delay(5000, CancellationToken.None);
                            d?.Dispose();
                            tcs.SetResult();
                            return;
                        }
                        return;
                    }, AwaitOperation.Switch);
                tcs.Task.Wait();
            });
            Thread.Sleep(3000);
            behaviorSubject.OnNext(true);
            R3.ReactiveCommand reactiveCommand = new R3.ReactiveCommand();

            testR.Wait();
            return 0;



            //  List<int>? aabb = null;
            // bool t1 = false;
            // var t2 = new object(); 
            //  List<int> res = System.Threading.LazyInitializer.EnsureInitialized<List<int>>(target:ref aabb,valueFactory:()=> aabb);
            // Test(() => aabb);
            //   aabb = new List<int>() { 1 };
            //  t1 = true;

            //Thread.Sleep(10_000);
            //int valueDelay = 0;
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Restart();
            //await Parallel.ForEachAsync(GetNumbersAsync(CancellationToken.None), (item, ct) =>
            // {
            //     Console.WriteLine("Iterations {0}, Delay = {1}", item.Iteration, item.Delay);
            //     valueDelay += item.Delay;
            //     return ValueTask.CompletedTask;
            // });
            //stopwatch.Stop();
            //Console.WriteLine("OnComplete {0} milliseconds, CurrentDelay = {1}", stopwatch.ElapsedMilliseconds, valueDelay);
        }

        private static void Test(Func<List<int>> aab)
        {
            _ = Task.Run(() =>
            {
                Thread.Sleep(3000);
                List<int>? test = aab.Invoke();
                if(test is null) System.Diagnostics.Debugger.Break();
                else 
                {
                   test.Add(2);
                  System.Diagnostics.Debugger.Break();
                }
            });            
        }

        public static async IAsyncEnumerable<(int Iteration,int Delay)> GetNumbersAsync([EnumeratorCancellation]CancellationToken cancellationToken)
        {
            int[] ints = new int[10];
            Array.Fill(ints, 1);

            Parallel.Invoke(() => { }, () => _ = Task.FromResult(1));

            ParallelQuery<int> test4 = ints.AsParallel().AsUnordered();

            ParallelQuery<int> res = ParallelEnumerable.Range(1, 5_000_000);

           // ParallelQuery<(int Iteration1, int Delay1)> values = new ParallelQuery<(int Iteration1, int Delay1)>();

            // ParallelEnumerable.ForAll()

            // var tes2 = ParallelEnumerable.Where(,)


            for(int i = 0;i < 10;i++)
            {
                int d = Random.Shared.Next(20, 4_444);
                await Task.Delay(d, cancellationToken); 
                yield return (i,d);
            }
        }
    }
}
