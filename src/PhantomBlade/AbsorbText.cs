using System;

namespace PhantomBlade
{
     
    // 형식 문자열은 StringsTable 의 "Absorb.LogLine" / "Absorb.Overhead" 한 곳에 있고, 여기서는 자리표와 값만 검사해 채운다.
    // 흡수 계산(StandAbsorb·StandRules)은 건드리지 않는다 — 이 파일은 이미 끝난 흡수의 결과(흡수량·남은 풀)를 글로 바꿀 뿐이다.
    public static class AbsorbText
    {
         
        // 흡수량 0 은 로그를 내지 않는 경우(StandAbsorb 의 정당한 무동작)라 여기 오면 호출 오류다 → 던진다.
        public static string LogLine(string format, int absorbed, int poolLeft)
        {
            RequireSlots(format, "LogLine", 2);
            if (absorbed < 1) throw new ArgumentOutOfRangeException("absorbed", absorbed, "PhantomBlade: AbsorbText.LogLine needs absorbed >= 1 (no line for a zero absorb)");
            if (poolLeft < 0) throw new ArgumentOutOfRangeException("poolLeft", poolLeft, "PhantomBlade: AbsorbText.LogLine needs poolLeft >= 0");
            return string.Format(format, absorbed, poolLeft);
        }

        // 머리 위 짧은 글(엔진 UIAccess.Bark). format 자리표: {0} = 흡수량.
        public static string Overhead(string format, int absorbed)
        {
            RequireSlots(format, "Overhead", 1);
            if (absorbed < 1) throw new ArgumentOutOfRangeException("absorbed", absorbed, "PhantomBlade: AbsorbText.Overhead needs absorbed >= 1 (no text for a zero absorb)");
            return string.Format(format, absorbed);
        }

        // 자리표 {0}..{n-1} 이 모두 있고 그 밖의 번호는 없어야 한다(문자열 표를 고치다 자리표를 빠뜨리거나 바꾸면 조용히 다른 숫자가 찍히지 않게).
         
        static void RequireSlots(string format, string who, int n)
        {
            if (string.IsNullOrEmpty(format)) throw new ArgumentException("PhantomBlade: AbsorbText." + who + " format is null or empty", "format");
            var got = PlaceholderIndices(format, who);
            for (int i = 0; i < n; i++)
                if (!got.Contains(i)) throw new ArgumentException("PhantomBlade: AbsorbText." + who + " format lacks slot {" + i + "}: '" + format + "'", "format");
            foreach (int i in got)
                if (i >= n) throw new ArgumentException("PhantomBlade: AbsorbText." + who + " format has an unexpected slot {" + i + "}: '" + format + "'", "format");
        }

        // .NET 복합 형식의 치환 항목 번호 집합. "{{"·"}}" = 리터럴 중괄호(자리표 아님, string.Format 규칙).
        // 받는 항목 모양(엄격한 부분집합) = '{' 숫자+ [ ',' '-'? 숫자+ ] [ ':' 중괄호 없는 글자* ] '}'. 그 밖(닫히지 않은 '{', 짝 없는 '}', 빈·숫자 아닌 번호,
        // 번호 뒤 공백, 형식 안의 중괄호)은 형식 오류 → ArgumentException(string.Format 의 FormatException 으로 새기 전에). 시험이 받은 형식마다 string.Format 결과와 대조한다.
        internal static System.Collections.Generic.HashSet<int> PlaceholderIndices(string format, string who)
        {
            var set = new System.Collections.Generic.HashSet<int>();
            int k = 0;
            while (k < format.Length)
            {
                char c = format[k];
                if (c == '}')
                {
                    if (k + 1 < format.Length && format[k + 1] == '}') { k += 2; continue; }
                    throw Malformed(format, who, k, "unmatched '}'");
                }
                if (c != '{') { k++; continue; }
                if (k + 1 < format.Length && format[k + 1] == '{') { k += 2; continue; }
                int j = k + 1;
                int start = j;
                while (j < format.Length && format[j] >= '0' && format[j] <= '9') j++;
                if (j == start) throw Malformed(format, who, k, "slot without a numeric index");
                if (j - start > 6) throw Malformed(format, who, k, "slot index too long");
                int index = int.Parse(format.Substring(start, j - start), System.Globalization.CultureInfo.InvariantCulture);
                if (j < format.Length && format[j] == ',')
                {
                    j++;
                    if (j < format.Length && format[j] == '-') j++;
                    int a = j;
                    while (j < format.Length && format[j] >= '0' && format[j] <= '9') j++;
                    if (j == a) throw Malformed(format, who, k, "alignment without digits");
                }
                if (j < format.Length && format[j] == ':')
                {
                    j++;
                    while (j < format.Length && format[j] != '}' && format[j] != '{') j++;
                }
                if (j >= format.Length || format[j] != '}') throw Malformed(format, who, k, "slot not closed by '}'");
                set.Add(index);
                k = j + 1;
            }
            return set;
        }

        static ArgumentException Malformed(string format, string who, int at, string why)
        {
            return new ArgumentException("PhantomBlade: AbsorbText." + who + " format is malformed at " + at + " (" + why + "): '" + format + "'", "format");
        }
    }
}
