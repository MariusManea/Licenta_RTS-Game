using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;
using System;

public class UserInput : MonoBehaviour
{
    private Player player;
    private Vector3 newCameraPosition;
    private Quaternion newCameraRotation;
    private Vector3 newCameraZoom;

    public Transform cameraRig;
    public Transform cameraTransform;

    // Start is called before the first frame update
    void Start()
    {
        player = transform.GetComponent<Player>();
        cameraRig = Camera.main.transform.root;
        cameraTransform = Camera.main.transform;
        newCameraPosition = cameraRig.position;
        newCameraRotation = cameraRig.rotation;
        newCameraZoom = cameraTransform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (player.isHuman)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) OpenPauseMenu();
            MoveCamera();
            RotateCamera();
            MouseActivity();
        }
    }

    private void OpenPauseMenu()
    {
        Time.timeScale = 0.0f;
        GetComponentInChildren<PauseMenu>().enabled = true;
        GetComponent<UserInput>().enabled = false;
        Cursor.visible = true;
        ResourceManager.MenuOpen = true;
    }

    private void RotateCamera()
    {
        if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftAlt))
        {
            float movement = Input.GetAxis("Mouse X");
            newCameraRotation *= Quaternion.Euler(Vector3.up * (movement * ResourceManager.RotateSpeed));
            if (newCameraRotation != cameraRig.rotation)
            {
                cameraRig.rotation = Quaternion.Lerp(cameraRig.rotation, newCameraRotation, Time.deltaTime * ResourceManager.CameraMovementTime);

            }
        }
    }

    private void MoveCamera()
    {
        if (!Input.GetMouseButton(1))
        {
            float xpos = Input.mousePosition.x;
            float ypos = Input.mousePosition.y;
            float xkeys = Input.GetAxisRaw("Horizontal");
            float ykeys = Input.GetAxisRaw("Vertical");
            bool fastCamera = false;
            bool mouseScroll = false;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                fastCamera = true;
            }
            //horizontal camera movement
            if ((xpos >= 0 && xpos < ResourceManager.ScrollWidth) || xkeys < 0)
            {
                newCameraPosition += cameraRig.right * -(fastCamera ? ResourceManager.FastScrollSpeed : ResourceManager.NormalScrollSpeed);
                if (xpos >= 0 && xpos < ResourceManager.ScrollWidth)
                {
                    player.hud.SetCursorState(CursorState.PanLeft);
                    mouseScroll = true;
                }
            }
            else if ((xpos <= Screen.width && xpos > Screen.width - ResourceManager.ScrollWidth) || xkeys > 0)
            {
                newCameraPosition += cameraRig.right * (fastCamera ? ResourceManager.FastScrollSpeed : ResourceManager.NormalScrollSpeed);
                if (xpos <= Screen.width && xpos > Screen.width - ResourceManager.ScrollWidth)
                {
                    player.hud.SetCursorState(CursorState.PanRight);
                    mouseScroll = true;
                }
            }

            //vertical camera movement
            if ((ypos >= 0 && ypos < ResourceManager.ScrollWidth) || ykeys < 0)
            {
                newCameraPosition += cameraRig.forward * -(fastCamera ? ResourceManager.FastScrollSpeed : ResourceManager.NormalScrollSpeed);
                if (ypos >= 0 && ypos < ResourceManager.ScrollWidth)
                {
                    player.hud.SetCursorState(CursorState.PanDown);
                    mouseScroll = true;
                }
            }
            else if ((ypos <= Screen.height && ypos > Screen.height - ResourceManager.ScrollWidth) || ykeys > 0)
            {
                newCameraPosition += cameraRig.forward * (fastCamera ? ResourceManager.FastScrollSpeed : ResourceManager.NormalScrollSpeed);
                if (ypos <= Screen.height && ypos > Screen.height - ResourceManager.ScrollWidth)
                {
                    player.hud.SetCursorState(CursorState.PanUp);
                    mouseScroll = true;
                }
            }

            if (newCameraPosition != cameraRig.position)
            {
                cameraRig.position = Vector3.Lerp(cameraRig.position, newCameraPosition, Time.deltaTime * ResourceManager.CameraMovementTime);
            }

            if (!mouseScroll)
            {
                player.hud.SetCursorState(CursorState.Select);
            }
        }

        float zoomScale = Input.GetAxis("Mouse ScrollWheel");
        if (zoomScale < 0)
        {
            if (newCameraZoom.y < ResourceManager.MaxCameraHeight)
            {
                newCameraZoom -= ResourceManager.ZoomAmount;
            }
        }
        else
        {
            if (zoomScale > 0)
            {
                if (newCameraZoom.y > ResourceManager.MinCameraHeight)
                {
                    newCameraZoom += ResourceManager.ZoomAmount;
                }
            }
        }
        if (newCameraZoom != cameraTransform.localPosition)
        {
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newCameraZoom, Time.deltaTime * ResourceManager.CameraMovementTime);
        }
    }

    private void MouseActivity()
    {
        if (Input.GetMouseButtonDown(0)) LeftMouseClick();
        else if (Input.GetMouseButtonDown(1)) RightMouseClick();
        MouseHover();
    }
    private void LeftMouseClick()
    {
        if (player.hud.MouseInBounds())
        {
            if (player.IsFindingBuildingLocation())
            {
                if (player.CanPlaceBuilding())
                {
                    player.StartConstruction();
                }
            }
            else
            {
                GameObject hitObject = FindHitObject();
                Vector3 hitPoint = FindHitPoint();
                if (hitObject && hitPoint != ResourceManager.InvalidPosition)
                {
                    if (player.SelectedObject) player.SelectedObject.MouseClick(hitObject, hitPoint, player);
                    else if (!WorkManager.ObjectIsGround(hitObject))
                    {
                        WorldObjects worldObject = hitObject.transform.parent.GetComponent<WorldObjects>();
                        if (worldObject)
                        {
                            //we already know the player has no selected object
                            player.SelectedObject = worldObject;
                            worldObject.SetSelection(true, player.hud.GetPlayingArea());
                        }
                    }
                }
            }
        }
    }

    private GameObject FindHitObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) return hit.collider.gameObject;
        return null;
    }
    private Vector3 FindHitPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) return hit.point;
        return ResourceManager.InvalidPosition;
    }

    private void RightMouseClick()
    {
        if (player.hud.MouseInBounds() && !Input.GetKey(KeyCode.LeftAlt) && player.SelectedObject)
        {
            if (player.IsFindingBuildingLocation())
            {
                player.CancelBuildingPlacement();
            }
            else
            {
                player.SelectedObject.SetSelection(false, player.hud.GetPlayingArea());
                player.SelectedObject = null;
            }
        }
    }

    private void MouseHover()
    {
        if (player.hud.MouseInBounds())
        {
            if (player.IsFindingBuildingLocation())
            {
                player.FindBuildingLocation();
            }
            else
            {
                GameObject hoverObject = FindHitObject();
                if (hoverObject)
                {
                    if (player.SelectedObject) player.SelectedObject.SetHoverState(hoverObject);
                    else if (!WorkManager.ObjectIsGround(hoverObject))
                    {
                        Player owner = hoverObject.transform.root.GetComponent<Player>();
                        if (owner)
                        {
                            Unit unit = hoverObject.transform.parent.GetComponent<Unit>();
                            Building building = hoverObject.transform.parent.GetComponent<Building>();
                            if (owner.userName == player.userName && (unit || building)) player.hud.SetCursorState(CursorState.Select);
                        }
                    }
                }
            }
        }
    }
}
