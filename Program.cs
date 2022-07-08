using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace КочепасовТестовое
{
    internal class Program
    {
        /// <summary>
        /// Получает путь к текстовому файлу, вводимый пользователем.
        /// </summary>
        /// <returns>Строка с адресом текстового файла, или <see langword="null"/>,
        /// если файл не является текстовым или при попытке доступа произошла ошибка.</returns>
        static string GetFilePath()
        {
            string filepath;
            Console.WriteLine("Введите абсолютный путь к текстовому файлу для частотного анализа:");
            filepath = Console.ReadLine();
            if (File.Exists(filepath))
            {
                if (Path.GetExtension(filepath) == ".txt")
                    return filepath;
                else
                {
                    Console.WriteLine("Данный файл не является текстовым");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Не удается найти файл {0}", filepath);
                return null;
            }
        }

        /// <summary>
        /// Считывает весь текст из текстового файла, находящегося по переданному пути.
        /// </summary>
        /// <param name="filepath">Полный путь к файлу для чтения.</param>
        /// <returns>Строка, содержащая текст из файла, или <see langword="null"/>,
        /// если во время чтения произошла ошибка.</returns>
        static string GetText(string filepath)
        {
            try
            {
                string text;
                StreamReader sr = new StreamReader(filepath);
                text = sr.ReadToEnd();
                sr.Close();
                return text;
            }
            catch (Exception e)
            {
                Console.WriteLine("Произошла ошибка! {0}", e.Message);
                return null;
            }
        }

        /// <summary>
        /// Заменяет в переданной строке все символы, не являющиеся буквенными, на символ пробела, а также приводит все символы к нижнему регистру.
        /// </summary>
        /// <param name="text">Строка, из котрой необходимо удалить все небуквенные символы и привести к нижнему регистру.</param>
        /// <returns>Строка, в которой все небуквенные символы заменены на символ пробела, приведенная к нижнему регистру.</returns>
        static string SanitizeText(string text)
        {
            return Regex.Replace(Regex.Replace(text, @"[\W\d_]", " "), @"\s+", " ").ToLower();
        }

        /// <summary>
        /// Выполняет частотный анализ текста, находя все триплеты (три буквы, идущие подряд) в переданном тексте и подсчитывая их количество,
        /// сохраняя данные в виде словаря и сортируя полученный словарь по убыванию количества повторений триплетов.
        /// </summary>
        /// <param name="text">Текст для частотного анализа.</param>
        /// <returns>Отсортированный по убыванию количества триплетов словарь.</returns>
        static Dictionary<string, int> GetTriplets(string text)
        {
            // Разбиение текста на слова
            string[] words = text.Split(' ');
            Dictionary<string, int> tripletCount = new Dictionary<string, int>(words.Length / 10);
            try
            {
                // Создание массива для хранения задач параллельной обработки слов
                Task[] tasks = new Task[words.Length];
                // Блокирование объекта словаря для избежания состязания за ресурсы
                lock (tripletCount)
                {
                    int x = 0;
                    foreach (string word in words)
                    {
                        Task task = new Task(() =>
                        {
                            // Получение триплетов из слова
                            for (int i = 0; i < word.Length; i++)
                            {
                                if (word.Length - i <= 2)
                                    break;
                                string triplet = word.Substring(i, 3);
                                try
                                {
                                    // Если такого триплета еще нет в словаре, добавить его с количеством 1
                                    if (!tripletCount.ContainsKey(triplet) && triplet != null)
                                    {
                                        tripletCount.Add(triplet, 1);
                                    }
                                    // Если такой триплет уже присутствует, увеличить счетчик
                                    else
                                    {
                                        tripletCount[triplet]++;
                                    }
                                }
                                catch (ArgumentException) { }
                            }
                        });
                        // Добавление задачи в массив и запуск
                        tasks[x] = task;
                        x++;
                        task.Start();
                    }
                }
                // Ожидание завершения выполнения всех задач
                Task.WaitAll(tasks);
                // Сортировка словаря по убыванию количества повторений триплетов
                tripletCount = tripletCount.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                return tripletCount;
            }
            // В случае, если возникает исключение из-за null аргумента в словаре или добавления уже существующего ключа,
            // повторить выполнение, так как этот процесс выглядит случайным и не зависит от текста
            catch (ArgumentException) { return GetTriplets(text); }
        }

        static void Main(string[] args)
        {
            string filepath = GetFilePath();
            Stopwatch st = new Stopwatch();
            st.Start();
            if (filepath != null)
            {
                string text = GetText(filepath);
                if (text != null)
                {
                    string cleanText = SanitizeText(text);
                    StringBuilder result = new StringBuilder(40);
                    // Получение 10 самых часто встречающихся триплетов
                    foreach (KeyValuePair<string, int> pair in GetTriplets(cleanText).Take(10))
                    {
                        result.AppendFormat("{0},", pair.Key);
                    }
                    result.Remove(result.Length - 1, 1);
                    Console.WriteLine(result);
                }
            }
            st.Stop();
            Console.WriteLine("Время выполнения программы: {0} миллисекунд", st.ElapsedMilliseconds);
            Console.WriteLine("Программа завершена, нажмите любую кнопку, чтобы продолжить...");
            Console.ReadLine();
        }
    }
}
