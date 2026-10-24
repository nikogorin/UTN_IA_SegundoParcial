using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 30f;

    private Vector3 _direction;
    private readonly float _lifeTime = 2f;
    private float _currentLifeTime;
    public float Speed => speed;

    private void Start()
    {
        _currentLifeTime = 0;
    }

    private void Update()
    {
        if(_direction == null)
        {
            Destroy(gameObject);
            return;
        }

        Move();
        UpdateLifeTime();
    }

    public void Initialize(Vector3 targetPosition)
    {
        _direction = (targetPosition - transform.position).normalized;
        transform.forward = _direction;
    }

    private void Move()
    {
        transform.position += speed * Time.deltaTime * _direction;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.TryGetComponent<PreyAgent>(out var preyAgent))
            return;

        preyAgent.TakeDamage(preyAgent.CurrentHealth);

        Destroy(gameObject);
    }

    private void UpdateLifeTime()
    {
        _currentLifeTime += Time.deltaTime;
        if(_currentLifeTime > _lifeTime)
            Destroy(gameObject);
    }
}
