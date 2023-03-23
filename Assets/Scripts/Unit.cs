using InexperiencedDeveloper.Utils;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Unit : MonoBehaviour, ISelectable
{
    private NavMeshAgent m_agent;
    private Animator m_animator;

    [SerializeField] private GameObject m_selectedRing;
    public bool IsSelected { get; private set; } = false;

    public GameObject GetGameObject() => gameObject;

    public void OnSelect()
    {
        IsSelected = true;
        m_selectedRing.SetActive(true);
    }

    public void OnDeselect()
    {
        IsSelected = false;
        m_selectedRing.SetActive(false);
    }


    private void Awake()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && IsSelected)
        {
            if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 10000))
            {
                m_agent.SetDestination(hit.point);
            }
        }
        float moveSpeed = MathUtils.Remap01(m_agent.velocity.magnitude, 0, m_agent.speed);
        m_animator.SetFloat("moveSpeed", moveSpeed);
    }
}
