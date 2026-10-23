using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WaveguideProject.Coordinator
{
    // Обертка для задачи, чтобы связать результат с ожидающим клиентом
    class ClientTask
    {
        public string TaskJson { get; set; } = ""; // Исправлено здесь
        public TaskCompletionSource<string> ResultSource { get; set; } = new TaskCompletionSource<string>();
    }

    class Program
    {
        // Очередь невыполненных задач
        private static BlockingCollection<ClientTask> _taskQueue = new BlockingCollection<ClientTask>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Запуск сервера-координатора...");
            TcpListener listener = new TcpListener(IPAddress.Any, 8080);
            listener.Start();

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleConnectionAsync(client));
            }
        }

        static async Task HandleConnectionAsync(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[16384];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) return;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (message.StartsWith("WORKER_READY"))
                {
                    string nodeId = message.Split('|')[1];
                    Console.WriteLine($"[Координатор] Узел {nodeId} готов. Ждем задачу в очереди...");

                    // Воркер блокируется здесь, пока в очереди не появится задача
                    ClientTask pendingTask = _taskQueue.Take();

                    // Передаем параметры воркеру
                    byte[] taskBytes = Encoding.UTF8.GetBytes(pendingTask.TaskJson);
                    await stream.WriteAsync(taskBytes, 0, taskBytes.Length);

                    // Ждем матрицу результатов от воркера
                    buffer = new byte[1048576]; // 1MB буфер для большой матрицы
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string resultJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"[Координатор] Получен результат от {nodeId}");

                        // Перенаправляем результат ожидающему клиенту
                        pendingTask.ResultSource.SetResult(resultJson);
                    }
                }
                else
                {
                    // Подключился клиент с новой задачей
                    Console.WriteLine("[Координатор] Получена задача от клиента.");
                    var clientTask = new ClientTask { TaskJson = message };
                    _taskQueue.Add(clientTask);

                    // Ожидаем, пока воркер посчитает и вернет ответ (асинхронно, сервер не виснет)
                    string result = await clientTask.ResultSource.Task;

                    // Возвращаем результат в WPF-клиент
                    byte[] response = Encoding.UTF8.GetBytes(result);
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
        }
    }
}