using UnityEngine;
using Cinemachine;

public class CinemachineSwitcher : MonoBehaviour
{
    public CinemachineVirtualCamera firstPersonCam;
    public CinemachineVirtualCamera thirdPersonCam;

    public void SetFirstPerson()
    {
        firstPersonCam.Priority = 10;
        thirdPersonCam.Priority = 0;
    }

    public void SetThirdPerson()
    {
        firstPersonCam.Priority = 0;
        thirdPersonCam.Priority = 10;
    }
}
