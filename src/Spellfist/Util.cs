using System;
using static WotrClassMod.Common.BlueprintUtil;

namespace Spellfist
{
    // 비공개 직렬화 필드 읽기(되읽기용). 필드가 없으면(게임 버전 차이) 조용히 null 을 돌려주지 않고 실패. 쓰기는 공용 BlueprintUtil.SetField.
    // 이름 리터럴은 ReflectionLookups 원장에 있어야 한다(시험이 소스의 GetF("...") 를 센다).
    internal static class Util
    {
        internal static object GetF(object o, string name)
        {
            if (o == null) throw new ArgumentNullException("o");
            var f = FindField(o.GetType(), name);
            if (f == null) throw new MissingFieldException("Spellfist: " + o.GetType().Name + "." + name + " not found");
            return f.GetValue(o);
        }
    }
}
