using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using WaveguideProject.Models;

namespace WaveguideProject.Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtResult.Text = "Подключение к серверу-координатору и отправка задачи...\n";
                BtnCalculate.IsEnabled = false; // Блокируем кнопку от двойного нажатия

                // 1. Собираем данные из интерфейса
                var parameters = new WaveguideParameters(
                    TaskId: "TASK_" + Guid.NewGuid().ToString().Substring(0, 4),
                    Width: double.Parse(txtWidth.Text),
                    Height: double.Parse(txtHeight.Text),
                    GridResolution: int.Parse(txtGrid.Text),
                    WalkersCount: int.Parse(txtWalkers.Text)
                );

                // 2. Открываем TCP-соединение
                using TcpClient client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", 8080);
                using NetworkStream stream = client.GetStream();

                // 3. Сериализация в JSON и отправка
                string taskJson = JsonSerializer.Serialize(parameters);
                byte[] taskBytes = Encoding.UTF8.GetBytes(taskJson);
                await stream.WriteAsync(taskBytes, 0, taskBytes.Length);

                txtResult.Text += "Ожидание выполнения задачи на вычислительном узле...\n";

                // 4. Ожидание ответа с результатом (размер буфера берем с запасом)
                byte[] buffer = new byte[1048576];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                string resultJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var result = JsonSerializer.Deserialize<CalculationResult>(resultJson);

                // 5. Вывод результатов
                txtResult.Text += $"\n[УСПЕХ] Выполнил узел: {result.NodeId}\n";
                txtResult.Text += $"Затрачено времени: {result.ComputeTime.TotalMilliseconds:F2} мс\n";
                txtResult.Text += $"Сгенерирована матрица поля размером: {result.Field.Length} x {result.Field[0].Length}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
                txtResult.Text = "Сбой сети или неверные данные.";
            }
            finally
            {
                BtnCalculate.IsEnabled = true; // Разблокируем кнопку
            }
        }
    }
}