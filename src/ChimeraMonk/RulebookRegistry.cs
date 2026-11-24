using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Kingmaker.PubSubSystem;

namespace ChimeraMonk
{
    // 규칙 처리기(IGlobalRulebookHandler<T>) 구독 판정. 2026-11-15 골렘술사 게임 로그 결함: 「subscription did not register (global False, hook True)」 매 틱 반복.
    // EventBus.IsGloballySubscribed(x) = GlobalSubscribers.Contains(x as IGlobalSubscriber) (Assembly-CSharp IL @0001-0012). GlobalSubscribers 는 IGlobalSubscriber 를 상속한
    // 비표식 인터페이스에만 칸을 만든다 → IGlobalRulebookHandler<T>(IGlobalSubscriber 를 상속하지 않음)만 구현한 객체는 실제로 구독돼 있어도 항상 거짓.
    // 규칙 구독의 실제 등록부: EventBus.Subscribe(object) @0044 → RulebookEventBus.Subscribe → GlobalRulebookSubscribers(RulebookSubscriptionManager<IGlobalRulebookSubscriber>).Subscribe @0001-00B0:
    //   T 마다(InterfaceFinder.GetSubscribedRulebookTypes = 구현한 IGlobalRulebookHandler<T> 의 T) m_Listeners[T] 를 찾거나 만들고(@002E-00A9) RulebookSubscribersList<T>.AddSubscriber(proxy ?? subscriber) (@00B0)
    //   → SubscribersList<IRulebookHandler<T>>.List.Add. 실행 중 해지는 칸을 null 로 비운다(RemoveSubscriber @0014-002D) → Contains 거짓 = 해지로 본다. dnlib/리플렉션 2026-11-15.
    // 필드 형식 잠금 = tests\ChimeraMonkTests.cs CheckRulebookSubscriptionProbes(이름은 ReflectionLookups 원장).
    internal static class RulebookRegistry
    {







    }
}
