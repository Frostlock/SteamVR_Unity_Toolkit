using UnityEngine;
using System.Collections;

public class SteamVR_BezierPointer : SteamVR_WorldPointer
{
    public float pointerLength = 10f;
    public int pointerDensity = 10;
    public bool showPointerCursor = true;
    public float pointerCursorRaduis = 0.5f;

    private Transform projectedBeamContainer;
    private Transform projectedBeamForward;
    private Transform projectedBeamJoint;
    private Transform projectedBeamDown;

    private GameObject pointerCursor;
    private CurveGenerator curvedBeam;

    // Use this for initialization
    protected override void Start()
    {
        base.Start();
        InitProjectedBeams();
        InitPointer();
        TogglePointer(false);
    }

    protected override void InitPointer()
    {
        pointerCursor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pointerCursor.name = "WorldPointer_BezierPointer_PointerCursor";
        pointerCursor.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pointerCursor.GetComponent<MeshRenderer>().receiveShadows = false;
        pointerCursor.transform.localScale = new Vector3(pointerCursorRaduis, 0.02f, pointerCursorRaduis);

        Destroy(pointerCursor.GetComponent<CapsuleCollider>());
        pointerCursor.layer = 2;

        GameObject global = new GameObject("WorldPointer_BezierPointer_CurvedBeamContainer");
        curvedBeam = global.gameObject.AddComponent<CurveGenerator>();
        curvedBeam.transform.parent = null;
        curvedBeam.Create(pointerDensity, pointerCursorRaduis);
        base.InitPointer();
    }

    protected override void SetPointerMaterial()
    {
        pointerCursor.GetComponent<MeshRenderer>().material = pointerMaterial;
        base.SetPointerMaterial();
    }

    protected override void TogglePointer(bool state)
    {
        base.TogglePointer(state);
        projectedBeamForward.gameObject.SetActive(state);
        projectedBeamJoint.gameObject.SetActive(state);
        projectedBeamDown.gameObject.SetActive(state);
    }

    protected override void DisablePointerBeam(object sender, ControllerClickedEventArgs e)
    {
        controllerIndex = e.controllerIndex;
        if (pointerContactTarget != null)
        {
            float destinationY = pointerContactTarget.transform.position.y + (pointerContactTarget.transform.localScale.y / 2) + 0.05f;
            destinationPosition = new Vector3(destinationPosition.x, destinationY, destinationPosition.z);
            base.PointerSet();
        }
        TogglePointer(false);
        curvedBeam.TogglePoints(false);
        pointerCursor.gameObject.SetActive(false);
    }

    private void InitProjectedBeams()
    {
        projectedBeamContainer = new GameObject("WorldPointer_BezierPointer_ProjectedBeamContainer").transform;
        projectedBeamContainer.transform.parent = this.transform;
        projectedBeamContainer.transform.localPosition = Vector3.zero;

        projectedBeamForward = new GameObject("WorldPointer_BezierPointer_ProjectedBeamForward").transform;
        projectedBeamForward.transform.parent = projectedBeamContainer.transform;

        projectedBeamJoint = new GameObject("WorldPointer_BezierPointer_ProjectedBeamJoint").transform;
        projectedBeamJoint.transform.parent = projectedBeamContainer.transform;
        projectedBeamJoint.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        projectedBeamDown = new GameObject("WorldPointer_BezierPointer_ProjectedBeamDown").transform;
    }

    private float GetForwardBeamLength()
    {
        float actualLength = pointerLength;
        Ray pointerRaycast = new Ray(transform.position, transform.forward);
        RaycastHit collidedWith;
        bool hasRayHit = Physics.Raycast(pointerRaycast, out collidedWith);

        //reset if beam not hitting or hitting new target
        if (!hasRayHit || (pointerContactTarget && pointerContactTarget != collidedWith.transform))
        {
            pointerContactDistance = 0f;
        }

        //check if beam has hit a new target
        if (hasRayHit)
        {
            pointerContactDistance = collidedWith.distance;
        }

        //adjust beam length if something is blocking it
        if (hasRayHit && pointerContactDistance < pointerLength)
        {
            actualLength = pointerContactDistance;
        }

        return actualLength;
    }

    private void ProjectForwardBeam()
    {
        float setThicknes = 0.01f;
        float setLength = GetForwardBeamLength();
        //if the additional decimal isn't added then the beam position glitches
        float beamPosition = setLength / (2 + 0.00001f);

        if (pointerFacingAxis == AxisType.XAxis)
        {
            projectedBeamForward.transform.localScale = new Vector3(setLength, setThicknes, setThicknes);
            projectedBeamForward.transform.localPosition = new Vector3(beamPosition, 0f, 0f);
            projectedBeamJoint.transform.localPosition = new Vector3(setLength - (projectedBeamJoint.transform.localScale.x / 2), 0f, 0f);
        }
        else
        {
            projectedBeamForward.transform.localScale = new Vector3(setThicknes, setThicknes, setLength);
            projectedBeamForward.transform.localPosition = new Vector3(0f, 0f, beamPosition);
            projectedBeamJoint.transform.localPosition = new Vector3(0f, 0f, setLength - (projectedBeamJoint.transform.localScale.z / 2));
        }        
    }

    private void ProjectDownBeam()
    {
        projectedBeamDown.transform.position = new Vector3(projectedBeamJoint.transform.position.x, projectedBeamJoint.transform.position.y, projectedBeamJoint.transform.position.z);

        Ray projectedBeamDownRaycast = new Ray(projectedBeamDown.transform.position, Vector3.down);
        RaycastHit collidedWith;
        bool downRayHit = Physics.Raycast(projectedBeamDownRaycast, out collidedWith);

        if (!downRayHit || (pointerContactTarget && pointerContactTarget != collidedWith.transform))
        {
            if (pointerContactTarget != null)
            {
                base.PointerOut();
            }
            pointerContactTarget = null;
            destinationPosition = Vector3.zero;
        }

        if (downRayHit)
        {
            projectedBeamDown.transform.position = new Vector3(projectedBeamJoint.transform.position.x, projectedBeamJoint.transform.position.y - collidedWith.distance, projectedBeamJoint.transform.position.z);
            projectedBeamDown.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            pointerContactTarget = collidedWith.transform;
            destinationPosition = projectedBeamDown.transform.position;

            base.PointerIn();
        }
    }

    private void SetPointerCursor()
    {
        if (pointerContactTarget != null)
        {
            pointerCursor.gameObject.SetActive(showPointerCursor);
            pointerCursor.transform.position = projectedBeamDown.transform.position;
            base.SetPlayAreaCursorTransform(pointerCursor.transform.position);
            UpdatePointerMaterial(pointerHitColor);
        } else
        {
            UpdatePointerMaterial(pointerMissColor);
            pointerCursor.gameObject.SetActive(false);
        }
    }

    private void DisplayCurvedBeam()
    {
        Vector3[] beamPoints = new Vector3[]
        {
            this.transform.position,
            projectedBeamJoint.transform.position + new Vector3(0f, 1f, 0f),
            projectedBeamDown.transform.position,
            projectedBeamDown.transform.position,
        };
        curvedBeam.SetPoints(beamPoints, pointerMaterial);
        curvedBeam.TogglePoints(true);
        pointerCursor.gameObject.SetActive((showPointerCursor ? true : false));
    }

    private void Update()
    {
        if (projectedBeamForward.gameObject.activeSelf)
        {            
            ProjectForwardBeam();
            ProjectDownBeam();
            DisplayCurvedBeam();
            SetPointerCursor();
        }
    }
}