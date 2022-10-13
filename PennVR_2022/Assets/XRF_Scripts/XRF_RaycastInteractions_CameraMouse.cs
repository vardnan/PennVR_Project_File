using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class XRF_RaycastInteractions_CameraMouse : MonoBehaviour
{
    public float laserDistance = 100.0f;
    public GameObject raycastCamera;
    private bool dontHighlight;
    private Material[] tempMaterialsHigh;
    private Material[] matsHigh;
    private GameObject tempSelectedObject;
    private Ray myRay;
    public GameObject feetIcon;
    public GameObject pointerPrefab;


    private GameObject hitObject;
    private bool Clickable;
    private bool Teleportable;
    private Vector3 endPoint;
    private bool iGrabbedYou;
    private bool grabable;
    private GameObject grabbedObject;
    private float tempDistance;
    private Vector3 basePosObject;
    private Vector3 clickOrigin;


    [System.Serializable]
    public enum ClickType // your custom enumeration
    {
        CanvasMouseClick,
        ObjectForwardClick,
    };
    public ClickType camType = ClickType.CanvasMouseClick;  // this public var should appear as a drop down

    private void Start()
    {
        pointerPrefab = Instantiate(pointerPrefab);
        pointerPrefab.SetActive(false);
        feetIcon = Instantiate(feetIcon);
        feetIcon.SetActive(false);
    }

    void Update()
    {
        if (camType == ClickType.CanvasMouseClick)
        {
            myRay = raycastCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        }
        else if (camType == ClickType.ObjectForwardClick)
        {
            myRay = new Ray(raycastCamera.transform.position, raycastCamera.transform.forward);
        }


        if (Input.GetMouseButtonDown(0))
        {
            ClickTheButton();
        }
        if (Input.GetMouseButtonUp(0))
        {
            UnClickTheButton();
        }

        Vector3 origin = this.transform.position;
        Vector3 direction = this.transform.forward;
        RaycastHit myRayHit;
        if (Physics.Raycast(myRay, out myRayHit, laserDistance) && !IsPointerOverUIObject())
        {
            //i shot out a ray and it hit something
            //Debug.Log("I hit something");
            hitObject = myRayHit.transform.gameObject;

            pointerPrefab.SetActive(true);
            pointerPrefab.transform.position = myRayHit.point;

            if(iGrabbedYou)
            {
                endPoint = myRayHit.point;
                if (iGrabbedYou)
                {
                    Debug.Log("hey i grabbed is true");
                    Bounds overallBounds = GetBounds(grabbedObject);
                    Vector3 placeObjPos = overallBounds.center - grabbedObject.transform.position;
                    if (raycastCamera.transform.position.y > endPoint.y)
                    {//this changes how it snaps based on if it is above or below your eye height
                        grabbedObject.transform.position = endPoint - placeObjPos + new Vector3(0, overallBounds.extents.y, 0);
                    }
                    else
                    {
                        grabbedObject.transform.position = endPoint - placeObjPos + new Vector3(0, -overallBounds.extents.y, 0);
                    }
                }
            }
            else if (!hitObject.GetComponent<Collider>().isTrigger && hitObject.GetComponent<XRF_InteractionController>())
            {

                //i shot out a ray and hit something with an interaction controller
                Debug.Log("I hit something with an interaction controller on it");
                endPoint = myRayHit.point;


                if (hitObject.GetComponent<XRF_InteractionController>().isTeleporter)
                {
                    Debug.Log("this is a teleporter");
                    RayMissed();
                    Teleportable = true;
                    feetIcon.transform.position = endPoint;
                    feetIcon.SetActive(true);
                    grabable = false;
                }
                else if (hitObject.GetComponent<XRF_InteractionController>().isGrabbable)
                {
                    tempDistance = Vector3.Distance(origin, myRayHit.point);
                    RayHit(hitObject);
                    Clickable = false;
                    feetIcon.SetActive(false);
                    Teleportable = false;
                    Debug.Log("hey i hit a grabbable");
                    grabable = true;
                }
                else
                {
                    RayHit(hitObject);
                    Clickable = true;
                    feetIcon.SetActive(false);
                    Teleportable = false;
                    grabable = false;
                }
            }
            else
            {
                RayMissed();
                feetIcon.SetActive(false);
                Teleportable = false;
                grabable = false;
            }
        }
        else
        {
            pointerPrefab.SetActive(false);
            RayMissed();
            feetIcon.SetActive(false);
            Teleportable = false;
            grabable = false;
        }
    }

    public void ClickTheButton()
    {
        if (Clickable)
        {
            XRF_InteractionController[] myInteractions = hitObject.GetComponents<XRF_InteractionController>();
            foreach (XRF_InteractionController t in myInteractions)
            {
                t.DoTheThing();
            }
        }
        else if (grabable)
        {
            grabbedObject = hitObject;
            iGrabbedYou = true;
            grabbedObject.GetComponent<Collider>().enabled = false;

        }
        else if (Teleportable)
        {
            Debug.Log("Flythrough Camera doesn't teleport");
        }
    }
    public void UnClickTheButton()
    {
        if (iGrabbedYou)
        {
            if (grabbedObject.GetComponent<XRF_InteractionController>().originalPos != Vector3.zero)
            {
                grabbedObject.transform.position = grabbedObject.GetComponent<XRF_InteractionController>().originalPos;
            }

            iGrabbedYou = false;
            grabbedObject.GetComponent<Collider>().enabled = true;
            grabbedObject = null;
            RayMissed();//clear everything after you let go
        }
    }

    void RayHit(GameObject touchObject)
    {
        if (tempSelectedObject != touchObject)
        {
            if (tempSelectedObject != null)
            {
                UnHighlightObj(tempSelectedObject);
            }
        }
        tempSelectedObject = touchObject;
        HighlightObj(tempSelectedObject);
    }
    void RayMissed()
    {
        //Debug.Log("ray missed");
        if (tempSelectedObject != null)
        {
            UnHighlightObj(tempSelectedObject);
            tempSelectedObject = null;
        }
        
    }
    void HighlightObj(GameObject highlightThis)
    {
        MeshRenderer rend = highlightThis.transform.gameObject.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            if (!dontHighlight)
            {
                tempMaterialsHigh = highlightThis.transform.gameObject.GetComponent<MeshRenderer>().sharedMaterials;
                matsHigh = new Material[tempMaterialsHigh.Length];

                Material highlightMaterial = highlightThis.GetComponent<XRF_InteractionController>().HighlightMaterial;

                for (int i = 0; i < tempMaterialsHigh.Length; i++)
                {
                    matsHigh[i] = highlightMaterial;
                }
                highlightThis.transform.gameObject.GetComponent<MeshRenderer>().sharedMaterials = matsHigh;
                dontHighlight = true;
            }
        }
    }
    void UnHighlightObj(GameObject unHighlightThis)
    {
        MeshRenderer rend = unHighlightThis.GetComponent<MeshRenderer>();
        if (rend != null)
        {
            unHighlightThis.transform.gameObject.GetComponent<MeshRenderer>().sharedMaterials = tempMaterialsHigh;
            dontHighlight = false;
        }
    }

    private bool IsPointerOverUIObject()
    {
        if(UnityEngine.EventSystems.EventSystem.current)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }
        else
        {
            return false;
        }
    }



    private Bounds GetBounds(GameObject go)
    {
        Bounds goBounds = new Bounds(go.transform.position, Vector3.zero);
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0) goBounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            Bounds bounds = r.bounds;
            goBounds.Encapsulate(bounds);
        }
        return goBounds;
    }

}
