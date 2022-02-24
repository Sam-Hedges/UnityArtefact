using Cinemachine;
using UnityEngine;

public class ClosestGoalManager : MonoBehaviour
{
    private CinemachineTargetGroup targetGroup;
    [SerializeField] private Transform player;
    [SerializeField] private Transform homeGoal, awayGoal;
    private Vector3 homePos, awayPos;
    void Awake() {
        targetGroup = GetComponent<CinemachineTargetGroup>();
        homePos = homeGoal.position;
        awayPos = awayGoal.position;
    }

    void Update() {
        ChangeTarget();
    }

    private void ChangeTarget() {
        
        Vector3 position = player.position;
        float homeDist = Vector3.Distance(position, homePos);
        float awayDist = Vector3.Distance(position, awayPos);
        
        if (homeDist < awayDist) {
            foreach (CinemachineTargetGroup.Target target in targetGroup.m_Targets) {
                if (target.target == homeGoal) { return; }
            }
            targetGroup.RemoveMember(awayGoal);
            targetGroup.AddMember(homeGoal, 1, 2);
        }
        else {
            foreach (CinemachineTargetGroup.Target target in targetGroup.m_Targets) {
                if (target.target == awayGoal) { return; }
            }
            targetGroup.RemoveMember(homeGoal);
            targetGroup.AddMember(awayGoal, 1, 2);
        }
    }
}
