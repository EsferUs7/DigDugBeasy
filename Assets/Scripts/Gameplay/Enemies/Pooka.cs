using UnityEngine;

/// <summary>
/// Pooka: простий тип ворога, що наслідує EnemyBase.
/// Не додає специфічної логіки зараз — залишено як розширюваний клас.
/// </summary>
public class Pooka : EnemyBase
{
    // На даному етапі використовує поведінку EnemyBase.
    // Якщо пізніше потрібно — перевизначай GetNextMoveDirection() або інші методи.

    protected override void Awake()
    {
        base.Awake();
        // можна ініціалізувати специфічні компоненти/параметри для Pooka
    }

    protected virtual void Start()
    {
        // За замовчуванням нічого не робимо — реалізація існує, щоб нащадки могли викликати base.Start()
    }
}
