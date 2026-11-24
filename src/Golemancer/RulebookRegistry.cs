using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Kingmaker.PubSubSystem;

namespace Golemancer
{
    // 규칙 처리기(IGlobalRulebookHandler<T>) 구독 판정. 2026-11-15 골렘술사 게임 로그 결함: 「subscription did not register (global False, hook True)」 매 틱 반복.
    // EventBus.IsGloballySubscribed(x) = GlobalSubscribers.Contains(x as IGlobalSubscriber) (Assembly-CSharp IL @0001-0012). GlobalSubscribers 는 IGlobalSubscriber 를 상속한
    // 비표식 인터페이스에만 칸을 만든다 → IGlobalRulebookHandler<T>(IGlobalSubscriber 를 상속하지 않음)만 구현한 객체는 실제로 구독돼 있어도 항상 거짓.
    // 규칙 구독의 실제 등록부: EventBus.Subscribe(object) @0044 → RulebookEventBus.Subscribe → GlobalRulebookSubscribers(RulebookSubscriptionManager<IGlobalRulebookSubscriber>).Subscribe @0001-00B0:
    //   T 마다(InterfaceFinder.GetSubscribedRulebookTypes = 구현한 IGlobalRulebookHandler<T> 의 T) m_Listeners[T] 를 찾거나 만들고(@002E-00A9) RulebookSubscribersList<T>.AddSubscriber(proxy ?? subscriber) (@00B0)
    //   → SubscribersList<IRulebookHandler<T>>.List.Add. 실행 중 해지는 칸을 null 로 비운다(RemoveSubscriber @0014-002D) → Contains 거짓 = 해지로 본다. dnlib/리플렉션 2026-11-15.
    // 필드 형식 잠금 = tests\GolemancerTests.cs CheckRulebookSubscriptionProbes(이름은 ReflectionLookups 원장).
    internal static class RulebookRegistry
    {
        // 구현한 IGlobalRulebookHandler<T> 의 T 전부에서 등록부 목록에 subscriber 가 있으면 참, 하나라도 없으면 거짓.
        // 규칙 처리기를 하나도 구현하지 않았거나 등록부 필드가 이 게임 판에 없으면 throw — 판정 불가를 「구독됨/안 됨」으로 바꾸지 않는다.
        internal static bool IsGloballySubscribed(object subscriber)
        {
            if (subscriber == null) throw new ArgumentNullException("subscriber");
            var rules = subscriber.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IGlobalRulebookHandler<>))
                .Select(i => i.GetGenericArguments()[0]).Distinct().ToList();
            if (rules.Count == 0) throw new InvalidOperationException("RulebookRegistry: " + subscriber.GetType().FullName + " implements no IGlobalRulebookHandler<T> (use EventBus.IsGloballySubscribed for IGlobalSubscriber handlers)");
            var mgr = RulebookEventBus.GlobalRulebookSubscribers;
            if (mgr == null) throw new InvalidOperationException("RulebookRegistry: RulebookEventBus.GlobalRulebookSubscribers is null");
            var lf = mgr.GetType().GetField("m_Listeners", BindingFlags.NonPublic | BindingFlags.Instance);
            if (lf == null) throw new MissingFieldException("RulebookRegistry: RulebookSubscriptionManager.m_Listeners not found");
            var listeners = lf.GetValue(mgr) as IDictionary;
            if (listeners == null) throw new InvalidOperationException("RulebookRegistry: RulebookSubscriptionManager.m_Listeners is not a dictionary");
            foreach (var rule in rules)
            {
                var entry = listeners.Contains(rule) ? listeners[rule] : null;
                if (entry == null) return false; // 이 규칙 형식의 목록이 없다 = 아무도 구독하지 않았다(목록은 첫 구독 때 만든다 @0035-00A9)
                if (!SubscriberList(entry).Contains(subscriber)) return false;
            }
            return true;
        }

        static IList SubscriberList(object entry)
        {
            for (var t = entry.GetType(); t != null; t = t.BaseType)
            {
                if (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(SubscribersList<>)) continue;
                var f = t.GetField("List", BindingFlags.NonPublic | BindingFlags.Instance);
                if (f == null) throw new MissingFieldException("RulebookRegistry: SubscribersList.List not found");
                var list = f.GetValue(entry) as IList;
                if (list == null) throw new InvalidOperationException("RulebookRegistry: SubscribersList.List is not a list");
                return list;
            }
            throw new InvalidOperationException("RulebookRegistry: " + entry.GetType().FullName + " does not derive from SubscribersList<>");
        }
    }
}
