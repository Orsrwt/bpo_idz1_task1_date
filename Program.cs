using System;

// ============================================================================
//  Безопасность программного обеспечения. ИДЗ 1. Задание 1, вариант 9.
//  Класс Date для работы с датами в формате "год.месяц.день".
//
//  Дата представлена тремя полями типа unsigned int (uint): год, месяц, день.
//  Реализованы операции:
//    - вычисление даты через заданное количество дней от указанной;
//    - вычитание заданного количества дней из даты;
//    - определение високосного года;
//    - присвоение и получение отдельных частей (год, месяц, день);
//    - сравнение дат (равно, до, после);
//    - вычисление количества дней между датами.
//
//  По требованиям методички дополнительно используются:
//    - интерфейс (IDate);
//    - абстрактный класс (DateBase) с абстрактным методом Format();
//    - конструкторы и деструктор;
//    - обработка исключений (try/catch, собственные проверки через throw);
//    - ввод данных с клавиатуры.
// ============================================================================

namespace DateApp
{
    /// <summary>
    /// Интерфейс, описывающий операции над датой.
    /// </summary>
    interface IDate
    {
        uint Year { get; set; }
        uint Month { get; set; }
        uint Day { get; set; }

        bool IsLeapYear();          // текущий год високосный?
        Date AddDays(long count);   // дата через count дней
        Date SubtractDays(long count); // дата за count дней до
        long DaysBetween(Date other);  // количество дней между датами
    }

    /// <summary>
    /// Абстрактный базовый класс. Хранит поля даты и общую логику,
    /// оставляя формат вывода (Format) на усмотрение наследника.
    /// </summary>
    abstract class DateBase : IDate
    {
        // Поля экземпляра: год, месяц, день (беззнаковые целые).
        protected uint year;
        protected uint month;
        protected uint day;

        // Количество дней в месяцах невисокосного года (индекс 1..12).
        protected static readonly int[] DaysPerMonth =
            { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        // --- Свойства (get/set отдельных частей) с проверкой корректности ---

        public uint Year
        {
            get { return year; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException("Year", "Год должен быть больше 0.");
                year = value;
            }
        }

        public uint Month
        {
            get { return month; }
            set
            {
                if (value < 1 || value > 12)
                    throw new ArgumentOutOfRangeException("Month", "Месяц должен быть в диапазоне 1..12.");
                month = value;
            }
        }

        public uint Day
        {
            get { return day; }
            set
            {
                uint dim = (uint)DaysInMonth(year, month);
                if (value < 1 || value > dim)
                    throw new ArgumentOutOfRangeException("Day",
                        string.Format("День должен быть в диапазоне 1..{0} для {1}.{2}.", dim, month, year));
                day = value;
            }
        }

        // --- Вспомогательные статические методы календаря ---

        /// <summary>Проверка, является ли указанный год високосным.</summary>
        public static bool IsLeap(uint y)
        {
            return (y % 4 == 0 && y % 100 != 0) || (y % 400 == 0);
        }

        /// <summary>Количество дней в заданном месяце заданного года.</summary>
        public static int DaysInMonth(uint y, uint m)
        {
            if (m < 1 || m > 12)
                throw new ArgumentOutOfRangeException("m", "Месяц должен быть в диапазоне 1..12.");
            if (m == 2 && IsLeap(y))
                return 29;
            return DaysPerMonth[m];
        }

        public bool IsLeapYear()
        {
            return IsLeap(year);
        }

        // Абстрактный метод: формат текстового представления даты.
        public abstract string Format();

        // Абстрактные операции, реализуемые в наследнике.
        public abstract Date AddDays(long count);
        public abstract Date SubtractDays(long count);
        public abstract long DaysBetween(Date other);
    }

    /// <summary>
    /// Конкретный класс даты.
    /// Для арифметики дат используется перевод даты в порядковый номер дня
    /// (число дней от условной эпохи) и обратно — пролептический
    /// григорианский календарь.
    /// </summary>
    class Date : DateBase
    {
        // ------------------------- Конструкторы -------------------------

        /// <summary>Конструктор по умолчанию: 1 января 1 года.</summary>
        public Date()
        {
            year = 1;
            month = 1;
            day = 1;
        }

        /// <summary>Основной конструктор с полной проверкой корректности.</summary>
        public Date(uint y, uint m, uint d)
        {
            if (y == 0)
                throw new ArgumentOutOfRangeException("y", "Год должен быть больше 0.");
            if (m < 1 || m > 12)
                throw new ArgumentOutOfRangeException("m", "Месяц должен быть в диапазоне 1..12.");
            int dim = DaysInMonth(y, m);
            if (d < 1 || d > (uint)dim)
                throw new ArgumentOutOfRangeException("d",
                    string.Format("День должен быть в диапазоне 1..{0}.", dim));

            year = y;
            month = m;
            day = d;
        }

        /// <summary>Конструктор копирования.</summary>
        public Date(Date other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            year = other.year;
            month = other.month;
            day = other.day;
        }

        // -------------------------- Деструктор --------------------------

        ~Date()
        {
            // В консоли демонстрируем момент уничтожения объекта.
            Console.WriteLine("[Деструктор] Объект Date " + Format() + " уничтожен.");
        }

        // ------------------- Перевод даты <-> номер дня -----------------

        /// <summary>
        /// Порядковый номер дня для пролептического григорианского календаря.
        /// Возвращает число дней, прошедших с 0000-03-01 (алгоритм «дней с марта»).
        /// </summary>
        private long ToDayNumber()
        {
            long y = year;
            long m = month;
            long d = day;

            // Считаем март началом года, чтобы високосный день был последним.
            long a = (14 - m) / 12;
            long yy = y + 4800 - a;
            long mm = m + 12 * a - 3;

            return d + (153 * mm + 2) / 5 + 365 * yy
                     + yy / 4 - yy / 100 + yy / 400 - 32045;
        }

        /// <summary>Обратное преобразование: номер дня -> дата (Y, M, D).</summary>
        private static Date FromDayNumber(long jdn)
        {
            long a = jdn + 32044;
            long b = (4 * a + 3) / 146097;
            long c = a - (146097 * b) / 4;
            long dd = (4 * c + 3) / 1461;
            long e = c - (1461 * dd) / 4;
            long m = (5 * e + 2) / 153;

            long day = e - (153 * m + 2) / 5 + 1;
            long month = m + 3 - 12 * (m / 10);
            long year = 100 * b + dd - 4800 + (m / 10);

            if (year < 1)
                throw new OverflowException("Результат вычислений выходит за пределы допустимых дат (год < 1).");

            return new Date((uint)year, (uint)month, (uint)day);
        }

        // ----------------------- Операции над датой ----------------------

        public override Date AddDays(long count)
        {
            return FromDayNumber(ToDayNumber() + count);
        }

        public override Date SubtractDays(long count)
        {
            return FromDayNumber(ToDayNumber() - count);
        }

        public override long DaysBetween(Date other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            return Math.Abs(ToDayNumber() - other.ToDayNumber());
        }

        // ------------------------- Сравнение дат -------------------------

        /// <summary>-1 если this раньше other, 0 если равны, 1 если позже.</summary>
        public int CompareTo(Date other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            long a = ToDayNumber();
            long b = other.ToDayNumber();
            if (a < b) return -1;
            if (a > b) return 1;
            return 0;
        }

        public bool IsEqualTo(Date other)  { return CompareTo(other) == 0; }
        public bool IsBefore(Date other)   { return CompareTo(other) < 0; }
        public bool IsAfter(Date other)    { return CompareTo(other) > 0; }

        // Перегрузка операторов для наглядности сравнения.
        public static bool operator ==(Date a, Date b)
        {
            if (ReferenceEquals(a, null)) return ReferenceEquals(b, null);
            if (ReferenceEquals(b, null)) return false;
            return a.CompareTo(b) == 0;
        }
        public static bool operator !=(Date a, Date b) { return !(a == b); }
        public static bool operator <(Date a, Date b)  { return a.CompareTo(b) < 0; }
        public static bool operator >(Date a, Date b)  { return a.CompareTo(b) > 0; }

        public override bool Equals(object obj)
        {
            Date d = obj as Date;
            return d != null && CompareTo(d) == 0;
        }
        public override int GetHashCode()
        {
            return (int)(ToDayNumber());
        }

        // ---------------------- Текстовое представление ------------------

        public override string Format()
        {
            // Формат "год.месяц.день" согласно заданию.
            return string.Format("{0}.{1:00}.{2:00}", year, month, day);
        }

        public override string ToString() { return Format(); }
    }

    // ========================= Точка входа программы =========================

    class Program
    {
        /// <summary>Безопасный ввод даты с клавиатуры с обработкой ошибок.</summary>
        static Date ReadDate(string title)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine(title + " (в формате год месяц день, например 2024 2 29):");
                    Console.Write("  > ");
                    string line = Console.ReadLine();
                    if (line == null)
                        throw new FormatException("Ввод отсутствует.");

                    string[] parts = line.Trim().Split(new[] { ' ', '.', '-', '/', '\t' },
                                                       StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 3)
                        throw new FormatException("Нужно ввести ровно три числа: год, месяц, день.");

                    uint y = uint.Parse(parts[0]);
                    uint m = uint.Parse(parts[1]);
                    uint d = uint.Parse(parts[2]);

                    return new Date(y, m, d);
                }
                catch (FormatException)
                {
                    Console.WriteLine("  Ошибка: неверный формат ввода. Повторите.");
                }
                catch (OverflowException)
                {
                    Console.WriteLine("  Ошибка: число слишком большое. Повторите.");
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Console.WriteLine("  Ошибка: " + ex.Message + " Повторите.");
                }
            }
        }

        static long ReadLong(string prompt)
        {
            while (true)
            {
                try
                {
                    Console.Write(prompt);
                    return long.Parse(Console.ReadLine());
                }
                catch (Exception)
                {
                    Console.WriteLine("  Ошибка: введите целое число.");
                }
            }
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== ИДЗ 1. Задание 1, вариант 9. Класс Date ===\n");

            bool run = true;
            while (run)
            {
                Console.WriteLine("\nМеню:");
                Console.WriteLine("  1 - Определить, високосный ли год у даты");
                Console.WriteLine("  2 - Дата через N дней от указанной");
                Console.WriteLine("  3 - Дата за N дней до указанной");
                Console.WriteLine("  4 - Сравнить две даты (равно / до / после)");
                Console.WriteLine("  5 - Количество дней между двумя датами");
                Console.WriteLine("  6 - Изменить отдельные части даты (get/set)");
                Console.WriteLine("  0 - Выход");
                Console.Write("Выбор: ");

                string choice = Console.ReadLine();
                try
                {
                    switch (choice)
                    {
                        case "1":
                        {
                            Date d = ReadDate("Введите дату");
                            Console.WriteLine("Дата {0}: год {1} {2}.", d.Format(), d.Year,
                                d.IsLeapYear() ? "високосный" : "невисокосный");
                            break;
                        }
                        case "2":
                        {
                            Date d = ReadDate("Введите исходную дату");
                            long n = ReadLong("Сколько дней прибавить: ");
                            Date r = d.AddDays(n);
                            Console.WriteLine("{0} + {1} дн. = {2}", d.Format(), n, r.Format());
                            break;
                        }
                        case "3":
                        {
                            Date d = ReadDate("Введите исходную дату");
                            long n = ReadLong("Сколько дней вычесть: ");
                            Date r = d.SubtractDays(n);
                            Console.WriteLine("{0} - {1} дн. = {2}", d.Format(), n, r.Format());
                            break;
                        }
                        case "4":
                        {
                            Date a = ReadDate("Введите первую дату");
                            Date b = ReadDate("Введите вторую дату");
                            if (a.IsEqualTo(b))
                                Console.WriteLine("Даты равны.");
                            else if (a.IsBefore(b))
                                Console.WriteLine("{0} раньше, чем {1}.", a.Format(), b.Format());
                            else
                                Console.WriteLine("{0} позже, чем {1}.", a.Format(), b.Format());
                            break;
                        }
                        case "5":
                        {
                            Date a = ReadDate("Введите первую дату");
                            Date b = ReadDate("Введите вторую дату");
                            Console.WriteLine("Между {0} и {1}: {2} дн.", a.Format(), b.Format(), a.DaysBetween(b));
                            break;
                        }
                        case "6":
                        {
                            Date d = ReadDate("Введите дату");
                            Console.WriteLine("Текущее значение: " + d.Format());
                            d.Year = (uint)ReadLong("Новый год: ");
                            d.Month = (uint)ReadLong("Новый месяц: ");
                            d.Day = (uint)ReadLong("Новый день: ");
                            Console.WriteLine("Новое значение: " + d.Format());
                            break;
                        }
                        case "0":
                            run = false;
                            break;
                        default:
                            Console.WriteLine("Неизвестный пункт меню.");
                            break;
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Console.WriteLine("Ошибка данных: " + ex.Message);
                }
                catch (OverflowException ex)
                {
                    Console.WriteLine("Ошибка переполнения: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Непредвиденная ошибка: " + ex.Message);
                }
            }

            Console.WriteLine("\nЗавершение программы.");
            // Принудительно вызовем финализаторы для наглядности работы деструктора.
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
