using InexperiencedDeveloper.Utils;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class Unit : MonoBehaviour
{
    private NavMeshAgent m_agent;
    private Animator m_animator;

    private void Awake()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
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
