using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
 // Пространство имен с DTO-моделями
using WaveguideProject.Worker.Compute;
using WaveguideProject.Models;

namespace WaveguideProject.Worker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Запуск вычислительного узла (Worker)...");
            string nodeId = Guid.NewGuid().ToString().Substring(0, 8); // Уникальный ID

            while (true) // Бесконечный цикл опроса задач
            {
                try
                {
                    using TcpClient client = new TcpClient();
                    await client.ConnectAsync("127.0.0.1", 8080);

                    using NetworkStream stream = client.GetStream();

                    // Сообщаем серверу, что узел свободен
                    byte[] readyMessage = Encoding.UTF8.GetBytes($"WORKER_READY|{nodeId}");
                    await stream.WriteAsync(readyMessage, 0, readyMessage.Length);

                    // Ждем задачу
                    byte[] buffer = new byte[16384];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) continue;

                    string taskJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var parameters = JsonSerializer.Deserialize<WaveguideParameters>(taskJson);

                    Console.WriteLine($"[Узел {nodeId}] Старт расчета задачи {parameters.TaskId}...");

                    // Вызов математики
                    var result = MonteCarloSolver.CalculateWaveguide(parameters, nodeId);

                    // Отправляем результат обратно координатору
                    string resultJson = JsonSerializer.Serialize(result);
                    byte[] resultBytes = Encoding.UTF8.GetBytes(resultJson);
                    await stream.WriteAsync(resultBytes, 0, resultBytes.Length);

                    Console.WriteLine($"[Узел {nodeId}] Результат отправлен.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Сетевая ошибка: {ex.Message}. Повтор через 3 секунды...");
                }

                await Task.Delay(3000);
            }
        }
    }
}