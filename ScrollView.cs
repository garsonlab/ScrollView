/*
 * Unity Module By Garson(https://github.com/garsonlab)
 * -------------------------------------------------------------------
 * FileName: ScrollView
 * Date    : 2018/08/11
 * Version : v1.0
 * Describe: 无线滚动列表，添加onItemRender事件后，设置numItems数量后自动渲染
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Scroll View", 38)]
    [SelectionBase]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ScrollView : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILayoutGroup
    {
        #region 定义结构

        [Serializable]
        public class ScrollRectEvent : UnityEvent<Vector2> { }

        [Serializable]
        public class ScrollRenderEvent : UnityEvent<int, Transform> { }

        public enum MovementType
        {
            Unrestricted, // Unrestricted movement -- can scroll forever
            Elastic, // Restricted but flexible -- can go past the edges, but springs back in place
            Clamped, // Restricted movement where it's not possible to go past the edges
        }

        public enum ScrollbarVisibility
        {
            Permanent,
            AutoHide,
            AutoHideAndExpandViewport,
        }

        /// <summary>
        /// 滚动方向
        /// </summary>
        public enum MotionType
        {
            /// <summary>
            /// 横向
            /// </summary>
            Horizontal = 0,

            /// <summary>
            /// 竖向
            /// </summary>
            Vertical = 1
        }

        /// <summary>
        /// 自动吸附滑动
        /// </summary>
        [Serializable]
        public struct AttachSnap
        {
            /// <summary>
            /// 速度阈值
            /// </summary>
            public float VelocityThreshold;
            /// <summary>
            /// 
            /// </summary>
            public float Duration;
        }

        /// <summary>
        /// 自动滚动状态
        /// </summary>
        private class AutoScrollState
        {
            public bool Enable;
            public float Duration;
            public float StartTime;
            public Vector2 EndScrollPosition;
        }

        /// <summary>
        /// 单个渲染信息
        /// </summary>
        private class ItemInfo
        {
            public RectTransform transform;
            public int virtualIndex;
            public int actualIndex;
            public bool active;
            public bool renderable;


            public ItemInfo(RectTransform trans)
            {
                transform = trans;
                trans.anchorMin = Vector2.up;
                trans.anchorMax = Vector2.up;
                trans.pivot = Vector2.one * 0.5f;
                trans.gameObject.SetActive(false);
                virtualIndex = int.MaxValue;
                actualIndex = int.MaxValue;
                active = false;
            }

            public Vector3 position
            {
                set
                {
                    if (transform != null)
                        transform.anchoredPosition = value;
                }
            }

            public float scale
            {
                set
                {
                    if(transform != null)
                        transform.localScale = Vector3.one*value;
                }
            }

            public void UpdateActive()
            {
                if (transform.gameObject.activeSelf != active)
                    transform.gameObject.SetActive(active);
            }

            public void UpdateIndex(int virIdx, uint len)
            {
                virtualIndex = virIdx;

                int length = (int)len;
                if (length == 0)
                {
                    actualIndex = 0;
                    return;
                }


                if (virIdx < 0)
                {
                    actualIndex = (length - 1) + (virIdx + 1) % length;
                }
                else if (virIdx > len - 1)
                {
                    actualIndex = virIdx % length;
                }
                else
                {
                    actualIndex = virIdx;
                }

                transform.gameObject.name = actualIndex.ToString();
            }

            public void Destroy()
            {
                if (transform != null)
                    UnityEngine.Object.Destroy(transform.gameObject);
            }
        }
        #endregion

        #region 属性

        [SerializeField]
        private RectTransform m_ViewPort;
        public RectTransform viewport { get { return m_ViewPort; } set { m_ViewPort = value; SetDirtyCaching(); } }

        [SerializeField]
        private RectTransform m_Content;
        public RectTransform content { get { return m_Content; } set { m_Content = value; } }
        
        [SerializeField]
        private RectTransform m_Prefab;
        public RectTransform prefab { get { return m_Prefab; } set { SetPrefab(value); } }

        [SerializeField]
        private uint m_NumItems;
        public uint numItems { get { return m_NumItems; } set { SetNumItems(value); } }

        [SerializeField]
        private MotionType m_MotionType = MotionType.Vertical;
        public MotionType motionType { get { return m_MotionType; } set { m_MotionType = value; } }

        [SerializeField]
        private MovementType m_MovementType = MovementType.Elastic;
        public MovementType movementType { get { return m_MovementType; } set { m_MovementType = value; } }

        [SerializeField]
        private float m_Elasticity = 0.1f; // Only used for MovementType.Elastic
        public float elasticity { get { return m_Elasticity; } set { m_Elasticity = value; } }

        [SerializeField]
        private bool m_Inertia = true;
        public bool inertia { get { return m_Inertia; } set { m_Inertia = value; } }

        [SerializeField]
        private float m_DecelerationRate = 0.135f; // Only used when inertia is enabled
        public float decelerationRate { get { return m_DecelerationRate; } set { m_DecelerationRate = value; } }

        [SerializeField]
        private float m_ScrollSensitivity = 1.0f;
        public float scrollSensitivity { get { return m_ScrollSensitivity; } set { m_ScrollSensitivity = value; } }
        
        [SerializeField]
        private RectOffset m_Margin;
        public RectOffset margin { get { return m_Margin; } set { m_Margin = value; } }

        [SerializeField]
        private uint m_FixedCount = 1;
        public uint fixedCount { get { return m_FixedCount; } set { m_FixedCount = value; } }

        [SerializeField]
        private Vector2 m_ItemSize;
        public Vector2 itemSize { get { return m_ItemSize; } set { m_ItemSize = value; } }

        [SerializeField]
        private Vector2 m_Space;
        public Vector2 space { get { return m_Space; } set { m_Space = value; } }

        [SerializeField]
        private bool m_Loop;
        public bool loop { get { return m_Loop; } set { SetLoop(value); } }

        [SerializeField]
        private bool m_AutoAttach;
        public bool autoAttach { get { return m_AutoAttach; } set { m_AutoAttach = value; } }

        [SerializeField]
        private AttachSnap m_AttachSnap = new AttachSnap() { VelocityThreshold = 0.5f, Duration = 0.3f };
        public AttachSnap attachSnap { get { return m_AttachSnap; } set { m_AttachSnap = value; } }

        [SerializeField]
        private bool m_ScaleCurve;
        public bool scaleCurve { get { return m_ScaleCurve; } set { m_ScaleCurve = value; } }

        [SerializeField]
        private AnimationCurve m_ScaleAnimation = new AnimationCurve(new Keyframe[] {new Keyframe(0,1), new Keyframe(1,0.5f)});
        public AnimationCurve scaleAnimation { get { return m_ScaleAnimation; } set { m_ScaleAnimation = value; } }

        [SerializeField]
        private Scrollbar m_HorizontalScrollbar;
        public Scrollbar horizontalScrollbar
        {
            get
            {
                return m_HorizontalScrollbar;
            }
            set
            {
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                m_HorizontalScrollbar = value;
                if (m_HorizontalScrollbar)
                    m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        [SerializeField]
        private Scrollbar m_VerticalScrollbar;
        public Scrollbar verticalScrollbar
        {
            get
            {
                return m_VerticalScrollbar;
            }
            set
            {
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                m_VerticalScrollbar = value;
                if (m_VerticalScrollbar)
                    m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        [SerializeField]
        private ScrollbarVisibility m_HorizontalScrollbarVisibility;
        public ScrollbarVisibility horizontalScrollbarVisibility { get { return m_HorizontalScrollbarVisibility; } set { m_HorizontalScrollbarVisibility = value; SetDirtyCaching(); } }

        [SerializeField]
        private ScrollbarVisibility m_VerticalScrollbarVisibility;
        public ScrollbarVisibility verticalScrollbarVisibility { get { return m_VerticalScrollbarVisibility; } set { m_VerticalScrollbarVisibility = value; SetDirtyCaching(); } }

        [SerializeField]
        private float m_HorizontalScrollbarSpacing;
        public float horizontalScrollbarSpacing { get { return m_HorizontalScrollbarSpacing; } set { m_HorizontalScrollbarSpacing = value; SetDirty(); } }

        [SerializeField]
        private float m_VerticalScrollbarSpacing;
        public float verticalScrollbarSpacing { get { return m_VerticalScrollbarSpacing; } set { m_VerticalScrollbarSpacing = value; SetDirty(); } }
       
        [SerializeField]
        private ScrollRectEvent m_OnValueChanged = new ScrollRectEvent();
        public ScrollRectEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        [SerializeField]
        private ScrollRenderEvent m_OnItemRender = new ScrollRenderEvent();
        public ScrollRenderEvent onItemRender { get { return m_OnItemRender; } set { m_OnItemRender = value; } }
        
        #endregion

        #region 滑动重构
        // The offset from handle position to mouse down position
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        private Vector2 m_ContentStartPosition = Vector2.zero;

        private Bounds m_ContentBounds;
        private Bounds m_ViewBounds;

        private Vector2 m_Velocity;
        public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

        private bool m_Dragging;

        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Bounds m_PrevViewBounds;
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;

        private bool m_HSliderExpand;
        private bool m_VSliderExpand;
        private float m_HSliderHeight;
        private float m_VSliderWidth;

        [NonSerialized]
        private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        private RectTransform m_HorizontalScrollbarRect;
        private RectTransform m_VerticalScrollbarRect;

        private DrivenRectTransformTracker m_Tracker;
        #endregion

        #region 虚拟列表数据

        private bool m_HasInit;//在点击运行后是否初始化
        private int m_TotalLine;//总行或列数
        private int m_MaxInit;//最大实例化数量
        private int m_StartIdx;//开始序号
        private int m_EndIdx;//结束序号
        private int m_CurLine;//当前行
        private List<ItemInfo> m_VirtualItems = new List<ItemInfo>();

        readonly AutoScrollState autoScrollState = new AutoScrollState();
        readonly Dictionary<int, ItemInfo> tmpContains = new Dictionary<int, ItemInfo>();
        readonly Queue<ItemInfo> tmpPool = new Queue<ItemInfo>();
        #endregion

        #region 列表重载

        protected ScrollView() {}

        void ICanvasElement.Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.Prelayout)
            {
                UpdateCachedData();
            }

            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();

                m_HasRebuiltLayout = true;
            }
        }

        void ICanvasElement.LayoutComplete()
        {
            //初始化RebuildList
            if (IsActive() && !m_HasInit && m_ViewPort.rect.size.x > 0 && m_ViewPort.rect.size.y > 0)
            {
                StartCoroutine(RebuildList());
            }
        }

        void ICanvasElement.GraphicUpdateComplete() { }

        void UpdateCachedData()
        {
            if (m_ViewPort == null)
                return;

            Transform transform = this.transform;
            m_HorizontalScrollbarRect = m_HorizontalScrollbar == null ? null : m_HorizontalScrollbar.transform as RectTransform;
            m_VerticalScrollbarRect = m_VerticalScrollbar == null ? null : m_VerticalScrollbar.transform as RectTransform;

            // These are true if either the elements are children, or they don't exist at all.
            bool viewIsChild = (m_ViewPort.parent == transform);
            bool hScrollbarIsChild = (!m_HorizontalScrollbarRect || m_HorizontalScrollbarRect.parent == transform);
            bool vScrollbarIsChild = (!m_VerticalScrollbarRect || m_VerticalScrollbarRect.parent == transform);
            bool allAreChildren = (viewIsChild && hScrollbarIsChild && vScrollbarIsChild);

            m_HSliderExpand = allAreChildren && m_HorizontalScrollbarRect && horizontalScrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport;
            m_VSliderExpand = allAreChildren && m_VerticalScrollbarRect && verticalScrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport;
            m_HSliderHeight = (m_HorizontalScrollbarRect == null ? 0 : m_HorizontalScrollbarRect.rect.height);
            m_VSliderWidth = (m_VerticalScrollbarRect == null ? 0 : m_VerticalScrollbarRect.rect.width);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
            if (m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);

            if (m_ViewPort == null)
            {
                var view = transform.Find("Viewport");
                if (view)
                    m_ViewPort = view as RectTransform;
                else
                    m_ViewPort = rectTransform;
            }

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (m_HorizontalScrollbar)
                m_HorizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
            if (m_VerticalScrollbar)
                m_VerticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);

            m_HasRebuiltLayout = false;
            m_Tracker.Clear();
            m_Velocity = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        public override bool IsActive()
        {
            return base.IsActive() && m_Content != null;
        }

        private void EnsureLayoutHasRebuilt()
        {
            if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }

        public void StopMovement()
        {
            m_Velocity = Vector2.zero;
        }

        void IScrollHandler.OnScroll(PointerEventData data)
        {
            if (!IsActive())
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;
            if (m_MotionType == MotionType.Vertical)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            if (m_MotionType == MotionType.Horizontal)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            Vector2 position = m_Content.anchoredPosition;
            position += delta * m_ScrollSensitivity;
            if (m_MovementType == MovementType.Clamped)
                position += CalculateOffset(position - m_Content.anchoredPosition);

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Velocity = Vector2.zero;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            UpdateBounds();

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ViewPort, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = m_Content.anchoredPosition;
            m_Dragging = true;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ViewPort, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            UpdateBounds();

            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            Vector2 offset = CalculateOffset(position - m_Content.anchoredPosition);
            position += offset;
            if (m_MovementType == MovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }

        protected void SetContentAnchoredPosition(Vector2 position)
        {
            if (m_MotionType != MotionType.Horizontal)
                position.x = m_Content.anchoredPosition.x;
            if (m_MotionType != MotionType.Vertical)
                position.y = m_Content.anchoredPosition.y;
            
            if (position != m_Content.anchoredPosition)
            {
                m_Content.anchoredPosition = position;
                UpdateBounds();
            }
        }

        protected void LateUpdate()
        {
            if (!m_Content)
                return;

            EnsureLayoutHasRebuilt();
            UpdateScrollbarVisibility();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (autoScrollState.Enable)//设置自动滚动
            {
                var alpha = Mathf.Clamp01((Time.unscaledTime - autoScrollState.StartTime) / Mathf.Max(autoScrollState.Duration, float.Epsilon));
                var interp = EaseInOutCubic(0, 1, alpha);
                var position = Vector2.Lerp(m_Content.anchoredPosition, autoScrollState.EndScrollPosition, interp);
                SetContentAnchoredPosition(position);
                if (Vector2.Distance(m_Content.anchoredPosition, autoScrollState.EndScrollPosition) <= 1)
                {
                    SetContentAnchoredPosition(autoScrollState.EndScrollPosition);
                    autoScrollState.Enable = false;
                }
            }
            else if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
            {
                Vector2 position = m_Content.anchoredPosition;
                if (m_MovementType == MovementType.Elastic && offset != Vector2.zero)
                {
                    if (m_AutoAttach)//自动吸附时滚动
                    {
                        ScrollTo(int.MaxValue, m_AttachSnap.Duration);
                    }
                    else
                    {
                        var speed = m_Velocity;
                        position = Vector2.SmoothDamp(m_Content.anchoredPosition, m_Content.anchoredPosition + offset, ref speed,
                            m_Elasticity, Mathf.Infinity, deltaTime);
                        m_Velocity = speed;
                    }
                }
                else if (m_Inertia)
                {
                    m_Velocity.x *= Mathf.Pow(m_DecelerationRate, deltaTime);
                    m_Velocity.y *= Mathf.Pow(m_DecelerationRate, deltaTime);
                    if (m_Velocity.sqrMagnitude < 10)
                        m_Velocity = Vector2.zero;
                    position += m_Velocity * deltaTime;

                    if (m_AutoAttach && m_Velocity.magnitude < m_AttachSnap.VelocityThreshold)
                    {
                        //中部滚动中，滚动速度小于预设值自动吸附
                        ScrollTo(int.MaxValue, m_AttachSnap.Duration);
                    }
                }
                else
                {
                    m_Velocity = Vector2.zero;
                }
                
                if (m_Velocity != Vector2.zero)
                {
                    if (m_MovementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position - m_Content.anchoredPosition);
                        position += offset;
                    }
                    SetContentAnchoredPosition(position);
                }
            }

            if (m_Dragging && m_Inertia)
            {
                Vector3 newVelocity = (m_Content.anchoredPosition - m_PrevPosition) / deltaTime;
                m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
            }

            if (m_ViewBounds != m_PrevViewBounds || m_ContentBounds != m_PrevContentBounds || m_Content.anchoredPosition != m_PrevPosition)
            {
                UpdateScrollbars(offset);
                m_OnValueChanged.Invoke(normalizedPosition);
                UpdatePrevData();

                UpdatePosition();
                
                if (m_ScaleCurve)
                {
                    float viewSize = GetViewSize();
                    int count = m_VirtualItems.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var item = m_VirtualItems[i];
                        if (item.active)
                        {
                            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(m_ViewPort, item.transform);
                            var ratio = Mathf.Abs(m_MotionType == MotionType.Horizontal ? bounds.center.x : bounds.center.y) / viewSize;
                            item.scale = m_ScaleAnimation.Evaluate(ratio);
                        }
                    }
                }
            }
        }
        #endregion

        #region 列表数据更新
        private void UpdatePrevData()
        {
            if (m_Content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = m_Content.anchoredPosition;
            m_PrevViewBounds = m_ViewBounds;
            m_PrevContentBounds = m_ContentBounds;
        }

        private void UpdateScrollbars(Vector2 offset)
        {
            if (m_HorizontalScrollbar)
            {
                if (m_ContentBounds.size.x > 0)
                    m_HorizontalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
                else
                    m_HorizontalScrollbar.size = 1;

                m_HorizontalScrollbar.value = horizontalNormalizedPosition;
            }

            if (m_VerticalScrollbar)
            {
                if (m_ContentBounds.size.y > 0)
                    m_VerticalScrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
                else
                    m_VerticalScrollbar.size = 1;

                m_VerticalScrollbar.value = verticalNormalizedPosition;
            }
        }

        public Vector2 normalizedPosition
        {
            get
            {
                return new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
            }
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        public float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (m_ContentBounds.size.x <= m_ViewBounds.size.x)
                    return (m_ViewBounds.min.x > m_ContentBounds.min.x) ? 1 : 0;
                return (m_ViewBounds.min.x - m_ContentBounds.min.x) / (m_ContentBounds.size.x - m_ViewBounds.size.x);
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        public float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if (m_ContentBounds.size.y <= m_ViewBounds.size.y)
                    return (m_ViewBounds.min.y > m_ContentBounds.min.y) ? 1 : 0;
                ;
                return (m_ViewBounds.min.y - m_ContentBounds.min.y) / (m_ContentBounds.size.y - m_ViewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }

        private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
        private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }

        private void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            // How much the content is larger than the view.
            float hiddenLength = m_ContentBounds.size[axis] - m_ViewBounds.size[axis];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = m_ViewBounds.min[axis] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            float newLocalPosition = m_Content.localPosition[axis] + contentBoundsMinPosition - m_ContentBounds.min[axis];

            Vector3 localPosition = m_Content.localPosition;
            if (Mathf.Abs(localPosition[axis] - newLocalPosition) > 0.01f)
            {
                localPosition[axis] = newLocalPosition;
                m_Content.localPosition = localPosition;
                m_Velocity[axis] = 0;
                UpdateBounds();
            }
        }

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private bool hScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                    return m_ContentBounds.size.x > m_ViewBounds.size.x + 0.01f;
                return true;
            }
        }
        private bool vScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                    return m_ContentBounds.size.y > m_ViewBounds.size.y + 0.01f;
                return true;
            }
        }

        public virtual void SetLayoutHorizontal()
        {
            m_Tracker.Clear();

            if (m_HSliderExpand || m_VSliderExpand)
            {
                m_Tracker.Add(this, m_ViewPort,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.SizeDelta |
                    DrivenTransformProperties.AnchoredPosition);

                // Make view full size to see if content fits.
                m_ViewPort.anchorMin = Vector2.zero;
                m_ViewPort.anchorMax = Vector2.one;
                m_ViewPort.sizeDelta = Vector2.zero;
                m_ViewPort.anchoredPosition = Vector2.zero;

                // Recalculate content layout with this size to see if it fits when there are no scrollbars.
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                m_ViewBounds = new Bounds(m_ViewPort.rect.center, m_ViewPort.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_VSliderExpand && vScrollingNeeded)
            {
                m_ViewPort.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), m_ViewPort.sizeDelta.y);

                // Recalculate content layout with this size to see if it fits vertically
                // when there is a vertical scrollbar (which may reflowed the content to make it taller).
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                m_ViewBounds = new Bounds(m_ViewPort.rect.center, m_ViewPort.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If it doesn't fit horizontally, enable horizontal scrollbar and shrink view vertically to make room for it.
            if (m_HSliderExpand && hScrollingNeeded)
            {
                m_ViewPort.sizeDelta = new Vector2(m_ViewPort.sizeDelta.x, -(m_HSliderHeight + m_HorizontalScrollbarSpacing));
                m_ViewBounds = new Bounds(m_ViewPort.rect.center, m_ViewPort.rect.size);
                m_ContentBounds = GetBounds();
            }

            // If the vertical slider didn't kick in the first time, and the horizontal one did,
            // we need to check again if the vertical slider now needs to kick in.
            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_VSliderExpand && vScrollingNeeded && m_ViewPort.sizeDelta.x == 0 && m_ViewPort.sizeDelta.y < 0)
            {
                m_ViewPort.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), m_ViewPort.sizeDelta.y);
            }
        }

        public virtual void SetLayoutVertical()
        {
            UpdateScrollbarLayout();
            m_ViewBounds = new Bounds(m_ViewPort.rect.center, m_ViewPort.rect.size);
            m_ContentBounds = GetBounds();
        }

        void UpdateScrollbarVisibility()
        {
            if (m_VerticalScrollbar && m_VerticalScrollbarVisibility != ScrollbarVisibility.Permanent && m_VerticalScrollbar.gameObject.activeSelf != vScrollingNeeded)
                m_VerticalScrollbar.gameObject.SetActive(vScrollingNeeded);

            if (m_HorizontalScrollbar && m_HorizontalScrollbarVisibility != ScrollbarVisibility.Permanent && m_HorizontalScrollbar.gameObject.activeSelf != hScrollingNeeded)
                m_HorizontalScrollbar.gameObject.SetActive(hScrollingNeeded);
        }

        void UpdateScrollbarLayout()
        {
            if (m_VSliderExpand && m_HorizontalScrollbar)
            {
                m_Tracker.Add(this, m_HorizontalScrollbarRect,
                    DrivenTransformProperties.AnchorMinX |
                    DrivenTransformProperties.AnchorMaxX |
                    DrivenTransformProperties.SizeDeltaX |
                    DrivenTransformProperties.AnchoredPositionX);
                m_HorizontalScrollbarRect.anchorMin = new Vector2(0, m_HorizontalScrollbarRect.anchorMin.y);
                m_HorizontalScrollbarRect.anchorMax = new Vector2(1, m_HorizontalScrollbarRect.anchorMax.y);
                m_HorizontalScrollbarRect.anchoredPosition = new Vector2(0, m_HorizontalScrollbarRect.anchoredPosition.y);
                if (vScrollingNeeded)
                    m_HorizontalScrollbarRect.sizeDelta = new Vector2(-(m_VSliderWidth + m_VerticalScrollbarSpacing), m_HorizontalScrollbarRect.sizeDelta.y);
                else
                    m_HorizontalScrollbarRect.sizeDelta = new Vector2(0, m_HorizontalScrollbarRect.sizeDelta.y);
            }

            if (m_HSliderExpand && m_VerticalScrollbar)
            {
                m_Tracker.Add(this, m_VerticalScrollbarRect,
                    DrivenTransformProperties.AnchorMinY |
                    DrivenTransformProperties.AnchorMaxY |
                    DrivenTransformProperties.SizeDeltaY |
                    DrivenTransformProperties.AnchoredPositionY);
                m_VerticalScrollbarRect.anchorMin = new Vector2(m_VerticalScrollbarRect.anchorMin.x, 0);
                m_VerticalScrollbarRect.anchorMax = new Vector2(m_VerticalScrollbarRect.anchorMax.x, 1);
                m_VerticalScrollbarRect.anchoredPosition = new Vector2(m_VerticalScrollbarRect.anchoredPosition.x, 0);
                if (hScrollingNeeded)
                    m_VerticalScrollbarRect.sizeDelta = new Vector2(m_VerticalScrollbarRect.sizeDelta.x, -(m_HSliderHeight + m_HorizontalScrollbarSpacing));
                else
                    m_VerticalScrollbarRect.sizeDelta = new Vector2(m_VerticalScrollbarRect.sizeDelta.x, 0);
            }
        }

        private void UpdateBounds()
        {
            if (m_ViewPort == null)
                return;

            m_ViewBounds = new Bounds(m_ViewPort.rect.center, m_ViewPort.rect.size);
            m_ContentBounds = GetBounds();

            if (m_Content == null)
                return;

            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 contentSize = m_ContentBounds.size;
            Vector3 contentPos = m_ContentBounds.center;
            Vector3 excess = m_ViewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (m_Content.pivot.x - 0.5f);
                contentSize.x = m_ViewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (m_Content.pivot.y - 0.5f);
                contentSize.y = m_ViewBounds.size.y;
            }

            m_ContentBounds.size = contentSize;
            m_ContentBounds.center = contentPos;
        }

        private readonly Vector3[] m_Corners = new Vector3[4];
        private Bounds GetBounds()
        {
            if (m_Content == null)
                return new Bounds();

            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            var toLocal = m_ViewPort.worldToLocalMatrix;
            m_Content.GetWorldCorners(m_Corners);
            for (int j = 0; j < 4; j++)
            {
                Vector3 v = toLocal.MultiplyPoint3x4(m_Corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (m_MovementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = m_ContentBounds.min;
            Vector2 max = m_ContentBounds.max;

            if (m_MotionType == MotionType.Horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;
                if (min.x > m_ViewBounds.min.x)
                    offset.x = m_ViewBounds.min.x - min.x;
                else if (max.x < m_ViewBounds.max.x)
                    offset.x = m_ViewBounds.max.x - max.x;
            }

            if (m_MotionType == MotionType.Vertical)
            {
                min.y += delta.y;
                max.y += delta.y;
                if (max.y < m_ViewBounds.max.y)
                    offset.y = m_ViewBounds.max.y - max.y;
                else if (min.y > m_ViewBounds.min.y)
                    offset.y = m_ViewBounds.min.y - min.y;
            }

            return offset;
        }

        protected void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        protected void SetDirtyCaching()
        {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirtyCaching();
        }
#endif

        #endregion

        #region 无限、虚拟列表重构
        private float GetViewSize()
        {
            return m_MotionType == MotionType.Vertical ? m_ViewPort.rect.height : m_ViewPort.rect.width;
        }

        private float GetCellSize(int axis = -1)
        {
            if (axis == -1)
            {
                axis = m_MotionType == MotionType.Horizontal ? 0 : 1;
            }

            return m_ItemSize[axis] + m_Space[axis];
        }

        private void UpdatePosition(bool forceRender = false)
        {
            int index = GetLoopIndex();
            if (index == m_CurLine && !forceRender)
                return;
            m_CurLine = index;

            m_StartIdx = m_CurLine * (int)m_FixedCount;
            m_EndIdx = m_StartIdx + m_MaxInit;

            if (!m_Loop)
            {
                if (m_EndIdx >= m_NumItems)
                    m_EndIdx = (int)m_NumItems;
            }

            tmpContains.Clear();
            tmpPool.Clear();

            int num = m_VirtualItems.Count;
            if(num <= 0)
                return;
            for (int i = 0; i < num; i++)
            {
                var item = m_VirtualItems[i];
                if (item.virtualIndex < m_StartIdx || item.virtualIndex >= m_EndIdx)
                {
                    tmpPool.Enqueue(item);
                    item.active = false;
                }
                else
                {
                    tmpContains.Add(item.virtualIndex, item);
                    item.active = true;
                }
                item.renderable = false;
            }
            //Debug.Log(string.Format("##渲染{0}\t({1}---{2})\t{3},{4}", m_CurLine, m_StartIdx, m_EndIdx, tmpPool.Count, tmpContains.Count));

            #region 渲染

            int line = m_CurLine;
            for (int i = m_StartIdx; i < m_EndIdx;)
            {
                for (int j = 0; j < m_FixedCount; j++)
                {
                    int virIdx = i + j;
                    if(virIdx >= m_EndIdx)
                        break;

                    if (!tmpContains.ContainsKey(virIdx))
                    {
                        var item = tmpPool.Dequeue();
                        if (m_MotionType == MotionType.Vertical)
                        {
                            Vector2 offset = new Vector2((j + 0.5f)*itemSize.x + j*m_Space.x,
                                                -(line + 0.5f)*itemSize.y - line*m_Space.y);
                            if (!m_Loop)
                                item.position = new Vector2(margin.left, -margin.top) + offset;
                            else
                                item.position = new Vector2(margin.left, 0) + offset;
                        }
                        else
                        {
                            Vector2 offset = new Vector2((line + 0.5f)*itemSize.x + line*m_Space.x,
                                                -(j + 0.5f)*itemSize.y - j*m_Space.y);
                            if (!m_Loop)
                                item.position = new Vector2(margin.left, margin.top) + offset;
                            else
                                item.position = new Vector2(0, margin.top) + offset;
                        }
                        item.UpdateIndex(virIdx, m_NumItems);
                        item.active = true;
                        item.renderable = true;
                    }
                    else
                    {
                        var item = tmpContains[virIdx];
                        item.active = true;
                        item.renderable = forceRender;
                    }
                }
                line++;
                i += (int)m_FixedCount;
            }

            #endregion
            
            for (int i = 0; i < num; i++)
            {
                var item = m_VirtualItems[i];
                item.UpdateActive();

                // Render在Active后
                if (item.renderable)
                    m_OnItemRender.Invoke(item.actualIndex, item.transform);
                item.renderable = false;
            }
        }

        private Vector2 CalculateClosestPosition(int target)
        {
            Vector2 offset = Vector2.zero;
            if (m_Loop)
            {
                if (m_MotionType == MotionType.Horizontal)
                {
                    int index = target != int.MaxValue
                        ? GetClosestLoopIndex(target)
                        : GetLoopIndex(-GetCellSize(0) * 0.5f);
                    offset.x = -(itemSize.x * index + (index - 1) * m_Space.x);
                }
                else
                {
                    int index = target != int.MaxValue
                        ? GetClosestLoopIndex(target)
                        : GetLoopIndex(GetCellSize(1) * 0.5f);
                    offset.y = itemSize.y * index + (index - 1) * m_Space.y;
                }
            }
            else
            {
                if (m_MotionType == MotionType.Horizontal)
                {
                    int index = target != int.MaxValue
                        ? GetClosestLoopIndex(target)
                        : GetLoopIndex(-GetCellSize(0) * 0.5f);
                    offset.x = -(margin.left + itemSize.x * index + (index - 1) * m_Space.x);
                }
                else
                {
                    int index = target != int.MaxValue
                        ? GetClosestLoopIndex(target)
                        : GetLoopIndex(GetCellSize(1) * 0.5f);
                    offset.y = margin.top + itemSize.y * index + (index - 1) * m_Space.y;
                }
            }
            return offset;
        }

        private int GetClosestLoopIndex(int index)
        {
            Vector2 offset = Vector2.zero;

            var diff = GetLoopPosition(index, (int)m_NumItems) - GetLoopPosition(m_CurLine, (int)m_NumItems);
            if (Mathf.Abs(diff) > m_NumItems / 2)
                diff = (int)Mathf.Sign(-diff) * ((int)m_NumItems - Mathf.Abs(diff));

            return diff + m_CurLine;
        }

        public void BackTop()
        {
            ScrollTo(0,0);
        }

        public void ScrollTo(int index, float duration)
        {
            velocity = Vector2.zero;

            autoScrollState.Enable = true;
            autoScrollState.Duration = duration;
            autoScrollState.StartTime = Time.unscaledTime;
            autoScrollState.EndScrollPosition = CalculateClosestPosition(index);
        }

        private int GetLoopPosition(int index, int length)
        {
            if (index < 0)
            {
                index = (length - 1) + (index + 1) % length;
            }
            else if (index > length - 1)
            {
                index = index % length;
            }
            return index;
        }

        private void SetPrefab(RectTransform rt)
        {
            //清理掉现在所有的预制
            //创建新的
            int num = m_VirtualItems.Count;
            for (int i = 0; i < num; i++)
            {
                if (m_VirtualItems.Count > 0)
                {
                    var item = m_VirtualItems[0];
                    m_VirtualItems.RemoveAt(0);
                    item.Destroy();
                }
            }

            m_ItemSize = Vector2.zero;

            m_Prefab = rt;
            if (rt == null)
                return;

            rt.gameObject.SetActive(false);
            Vector2 size = rt.sizeDelta;
            m_ItemSize.x = m_ItemSize.x > 0 ? m_ItemSize.x : size.x;
            m_ItemSize.y = m_ItemSize.y > 0 ? m_ItemSize.y : size.y;
        }

        private void SetNumItems(uint num)
        {
            m_NumItems = num;

            if (!Application.isPlaying || !m_HasInit || !m_Content || !m_Prefab || !m_ViewPort)
                return;
            int count = m_VirtualItems.Count;
            int init = 0;
            if (m_Loop)
                init = m_MaxInit;
            else
                init = Mathf.Min((int)num, m_MaxInit);
            if (count < init)
            {
                for (int i = count; i < init; i++)
                {
                    RectTransform item = CreateItem();
                    m_VirtualItems.Add(new ItemInfo(item));
                }
            }

            m_TotalLine = Mathf.CeilToInt(num * 1.0f / m_FixedCount);
            if (m_MotionType == MotionType.Horizontal)
            {
                float x = margin.left + margin.right + m_ItemSize.x * m_TotalLine +
                          m_Space.x * Mathf.Clamp(m_TotalLine, 0, m_TotalLine - 1);
                float y = margin.top + margin.bottom + m_ItemSize.y * m_FixedCount +
                          m_Space.y * Mathf.Clamp(m_FixedCount, 0, m_FixedCount - 1);

                m_Content.sizeDelta = new Vector2(x, m_ViewPort.rect.height > y ? m_ViewPort.rect.height : y);
            }
            else
            {
                float x = margin.left + margin.right + m_ItemSize.x * m_FixedCount +
                          m_Space.x * Mathf.Clamp(m_FixedCount, 0, m_FixedCount - 1);
                float y = margin.top + margin.bottom + m_ItemSize.y * m_TotalLine +
                          m_Space.y * Mathf.Clamp(m_TotalLine, 0, m_TotalLine - 1);
                m_Content.sizeDelta = new Vector2(m_ViewPort.rect.width > x ? m_ViewPort.rect.width : x, y);
            }

            RefreshList();
        }

        private void SetLoop(bool isLoop)
        {
            if (isLoop)
            {
                int count = m_VirtualItems.Count;
                if (count < m_MaxInit)
                {
                    for (int i = count; i < m_MaxInit; i++)
                    {
                        RectTransform item = CreateItem();
                        m_VirtualItems.Add(new ItemInfo(item));
                    }
                }
            }
            m_Loop = isLoop;
            RefreshList();
        }

        private IEnumerator RebuildList()
        {
            if (!Application.isPlaying || !m_HasInit || !m_Content || !m_Prefab || !m_ViewPort)
                yield return null;
            yield return new WaitForEndOfFrame();

            m_Content.anchorMin = new Vector2(0, 1);
            m_Content.anchorMax = new Vector2(0, 1);
            m_Content.pivot = new Vector2(0, 1);

            SetPrefab(m_Prefab);

            m_MaxInit = Mathf.CeilToInt(GetViewSize() / GetCellSize()) + 1;
            m_MaxInit *= (int)m_FixedCount;

            ContentSizeFitter filter = content.GetComponent<ContentSizeFitter>();
            if (filter != null)
                filter.enabled = false;
            LayoutGroup layout = content.GetComponent<LayoutGroup>();
            if (layout != null)
                layout.enabled = false;

            m_HasInit = true;
            SetNumItems(m_NumItems);
        }

        public void ResetList()
        {
            StartCoroutine(RebuildList());
        }

        public void RefreshList()
        {
            UpdatePosition(true);
        }

        private int GetLoopIndex(float offset = 0)
        {
            int index = 0;
            float pos = m_MotionType == MotionType.Horizontal
                ? m_Content.anchoredPosition.x
                : m_Content.anchoredPosition.y;
            pos = Mathf.Round(pos) + offset;
            if (m_Loop)
            {
                if (m_MotionType == MotionType.Horizontal)
                {
                    index = Mathf.FloorToInt(-pos / GetCellSize(0));
                }
                else
                {
                    index = Mathf.FloorToInt(pos / GetCellSize(1));
                }
            }
            else
            {
                if (m_MotionType == MotionType.Horizontal)
                {
                    if (pos >= 0)
                        index = 0;
                    else
                        index = Mathf.FloorToInt(Mathf.Abs(pos + m_Margin.left) / GetCellSize(0));
                }
                else
                {
                    if (pos < 0)
                        index = 0;
                    else
                        index = Mathf.FloorToInt(Mathf.Abs(pos - m_Margin.top) / GetCellSize(1));
                }
            }

            return index;
        }

        private RectTransform CreateItem()
        {
            if (m_Prefab)
            {
                GameObject obj = GameObject.Instantiate(m_Prefab.gameObject) as GameObject;
                obj.transform.SetParent(m_Content);
                obj.transform.localScale = Vector3.one;

                return obj.transform as RectTransform;
            }
            return null;
        }
        
        private float EaseInOutCubic(float start, float end, float value)//滑动曲线
        {
            value /= 0.5f;
            end -= start;
            if (value < 1f)
            {
                return end * 0.5f * value * value * value + start;
            }
            value -= 2f;
            return end * 0.5f * (value * value * value + 2f) + start;
        }
        
        #endregion
    }

}
