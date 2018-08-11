/*
 * Unity Module By Garson(https://github.com/garsonlab)
 * -------------------------------------------------------------------
 * FileName: GDropDown
 * Date    : 2018/08/11
 * Version : v1.0
 * Describe: 下拉列表， 支持无限循环滚动列表
 */
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/GDropdown", 100)]
    [RequireComponent(typeof (RectTransform))]
    [DisallowMultipleComponent]
    public class GDropDown : MonoBehaviour
    {
        #region Template
        [SerializeField]
        private Toggle m_CaptionToggle;
        /// <summary>
        /// Button of Whole Component
        /// </summary>
        public Toggle CaptionToggle { get { return m_CaptionToggle; } set { SetCaptionButton(value);} }

        [Tooltip("Display Text of Selected Item")]
        [SerializeField]
        private Text m_CaptionText;
        /// <summary>
        /// Display Text of Selected Item
        /// </summary>
        public Text CaptionText { get { return m_CaptionText; } set { m_CaptionText = value; } }

        [Tooltip("Display Image of Selected Item")]
        [SerializeField]
        private Image m_CaptionImage;
        /// <summary>
        /// Display Image of Selected Item
        /// </summary>
        public Image CaptionImage { get { return m_CaptionImage; } set { m_CaptionImage = value; } }
        [Tooltip("Flag is Open List")]
        [SerializeField]
        private Transform m_Flag;
        private float m_defaultAngle;
        #endregion
        [Space]
        #region Other
        [SerializeField]
        private ScrollView m_ScrollView;
        
        [SerializeField]
        private int m_SelectIndex;
        /// <summary>
        /// Current Select Index
        /// </summary>
        public int selectIndex { get { return m_SelectIndex; } set { SetSelectIndex(value); }}
        /// <summary>
        /// Current Select Value
        /// </summary>
        public string selectValue
        {
            get { return m_DropData.Count > m_SelectIndex ? m_DropData[m_SelectIndex].text : ""; }
            set
            {
                int count = m_DropData.Count;
                for (int i = 0; i < count; i++)
                {
                    if (m_DropData[i].text == value)
                    {
                        SetSelectIndex(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Drop Down Value Changed, Used to Lua
        /// </summary>
        public Action<int> OnValueChanged;
        
        [SerializeField]
        List<GDropData> m_DropData = new List<GDropData>();

        [Serializable]
        public class GDropDownEvent : UnityEvent<int> { }
        public GDropDownEvent onValueChanged = new GDropDownEvent();

        private Dictionary<Transform, GItem> m_Items = new Dictionary<Transform, GItem>();
        private RectTransform m_PointerMask;
        private Transform m_Canvas;
        #endregion

        void Awake()
        {
            if (m_Flag)
                m_defaultAngle = m_Flag.localEulerAngles.z;
            if (m_ScrollView)
                m_ScrollView.onItemRender.AddListener(OnItemRender);
            SetCaptionButton(m_CaptionToggle);
            m_CaptionToggle.isOn = false;
            SetSelectIndex(m_SelectIndex);
            CloseMask();
        }

        public void AddOptions(GDropData[] options)
        {
            for (int i = 0; i < options.Length; i++)
                this.m_DropData.Add(options[i]);

            if (m_CaptionToggle.isOn)
                OpenMask();
        }

        public void AddOptions(Sprite[] options)
        {
            for (int i = 0; i < options.Length; i++)
                this.m_DropData.Add(new GDropData(options[i]));

            if (m_CaptionToggle.isOn)
                OpenMask();
        }

        public void AddOptions(string[] options)
        {
            for (int i = 0; i < options.Length; i++)
                this.m_DropData.Add(new GDropData(options[i]));

            if (m_CaptionToggle.isOn)
                OpenMask();
        }

        public void RemoveAt(int index)
        {
            if(index < 0 || index >= m_DropData.Count)
                return;
            m_DropData.RemoveAt(index);
            if (index == m_SelectIndex)
            {
                SetSelectIndex(index - 1);
            }
        }

        public void RemoveValue(string value)
        {
            int index = -1;
            int count = m_DropData.Count;
            for (int i = 0; i < count; i++)
            {
                if (m_DropData[i].text == value)
                {
                    index = i;
                    break;
                }
            }
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        public void ClearOptions()
        {
            this.m_DropData.Clear();
            if (m_CaptionToggle.isOn)
                OpenMask();
        }


        /// <summary>
        /// Refresh Display View
        /// </summary>
        private void RefreshShowValue()
        {
            if (m_DropData.Count > m_SelectIndex)
            {
                GDropData data = m_DropData[m_SelectIndex];
                if (m_CaptionText)
                    m_CaptionText.text = data.text;
                if (m_CaptionImage)
                    m_CaptionImage.sprite = data.image;
            }
            else
            {
                if (m_CaptionText)
                    m_CaptionText.text = "";
                if (m_CaptionImage)
                    m_CaptionImage.sprite = null;
            }
        }

        /// <summary>
        /// Render Item in List
        /// </summary>
        /// <param name="index"></param>
        /// <param name="child"></param>
        private void OnItemRender(int index, Transform child)
        {
            GItem item;
            if (!m_Items.TryGetValue(child, out item))
                item = new GItem(this, child);

            if (m_DropData.Count > index)
            {
                item.Reset(m_DropData[index].text, m_DropData[index].image, index == m_SelectIndex);
            }
        }

        /// <summary>
        /// Set Cur Select Index When Click
        /// </summary>
        /// <param name="p"></param>
        private void SetSelectIndex(int p)
        {
            p = Mathf.Clamp(p, 0, m_DropData.Count);
            m_SelectIndex = p;
            RefreshShowValue();
            onValueChanged.Invoke(p);
            if (OnValueChanged != null)
                OnValueChanged(p);
            m_CaptionToggle.isOn = false;
        }

        /// <summary>
        /// Open Mask to Poniters Out of List
        /// </summary>
        private void OpenMask()
        {
            if (m_PointerMask == null) //Create Mask
            {
                GameObject o = new GameObject("Pointer Mask");
                o.transform.SetParent(transform);
                Image mask = o.AddComponent<Image>();
                mask.color = new Color(1, 1, 1, 0);
                m_PointerMask = o.transform as RectTransform;
                m_PointerMask.sizeDelta = new Vector2(Screen.width, Screen.height);
                Button btnMask = o.AddComponent<Button>();
                btnMask.onClick.AddListener(() =>
                {
                    m_CaptionToggle.isOn = false;
                });
            }

            if (m_Canvas == null) //Find Canvas
            {
                Canvas canvas = GameObject.FindObjectOfType<Canvas>();
                m_Canvas = canvas.transform;
            }

            m_PointerMask.gameObject.SetActive(true);
            m_PointerMask.SetParent(m_Canvas);
            m_PointerMask.localPosition = Vector3.zero;

            if (m_ScrollView != null)
            {
                m_ScrollView.transform.SetParent(m_Canvas);
                m_ScrollView.gameObject.SetActive(true);
                m_ScrollView.numItems = (uint)m_DropData.Count;
            }

            if (m_Flag)
                m_Flag.localEulerAngles = new Vector3(0, 0, m_defaultAngle+180);
        }

        /// <summary>
        /// Close Mask to Other Pointers
        /// </summary>
        private void CloseMask()
        {
            if (m_PointerMask != null)
            {
                m_PointerMask.transform.SetParent(transform);
                m_PointerMask.gameObject.SetActive(false);
            }

            if (m_ScrollView != null)
            {
                m_ScrollView.transform.SetParent(transform);
                m_ScrollView.gameObject.SetActive(false);
            }
            if (m_Flag)
                m_Flag.localEulerAngles = new Vector3(0, 0, m_defaultAngle);
        }
        

        /// <summary>
        /// Set Outter Button of Whole Component
        /// </summary>
        /// <param name="btn"></param>
        private void SetCaptionButton(Toggle btn)
        {
            if (m_CaptionToggle != null)
                m_CaptionToggle.onValueChanged.RemoveListener(OnCaptionButtonClicked);

            m_CaptionToggle = btn;
            if (m_CaptionToggle != null)
                m_CaptionToggle.onValueChanged.AddListener(OnCaptionButtonClicked);
        }

        /// <summary>
        /// On Caption Button Clicked
        /// </summary>
        private void OnCaptionButtonClicked(bool active)
        {
            if (active)
                OpenMask();
            else
                CloseMask();
        }

        /// <summary>
        /// Get Or Add Component on O
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        T GetOrAddComponent<T>(GameObject o) where T : Component
        {
            T com = o.GetComponent<T>();
            if (com == null)
                com = o.AddComponent<T>();
            return com;
        }

        /// <summary>
        /// Release All
        /// </summary>
        protected void OnDestroy()
        {
            //If Release on Drop State, Delete Mask
            //if (m_PointerMask != null)
            //    m_PointerMask.SetParent(transform);
            CloseMask();
        }

        /// <summary>
        /// Renderer Item in Endless List
        /// </summary>
        protected internal class GItem
        {
            GDropDown dropDown;
            Transform item;
            Button btn;

            Text text;
            Image image;
            GameObject selected;
            bool activeSelf;

            public string m_Text { get { return text.text; } set { text.text = value; } }
            public Sprite m_Image { get { return image.sprite; } set { image.sprite = value; } }

            public GItem(GDropDown parent, Transform item)
            {
                this.dropDown = parent;
                this.item = item;
                activeSelf = false;

                Transform t_trans = item.Find("title");
                if (t_trans)
                {
                    text = t_trans.gameObject.GetComponent<Text>();
                }
                Transform t_image = item.Find("icon");
                if (t_image)
                {
                    image = t_image.gameObject.GetComponent<Image>();
                }
                Transform t_selected = item.Find("selected");
                if (t_selected)
                {
                    selected = t_selected.gameObject;
                }

                btn = item.GetComponent<Button>();
                if (btn == null)
                {
                    Transform t_btn = item.Find("btn");
                    if (t_btn != null)
                        btn = dropDown.GetOrAddComponent<Button>(t_btn.gameObject);
                    else
                        btn = dropDown.GetOrAddComponent<Button>(item.gameObject);
                }

                btn.onClick.AddListener(OnBtnItemClicked);
            }

            private void OnBtnItemClicked()
            {
                if (!activeSelf)
                {
                    SetActive(true);
                    dropDown.SetSelectIndex(int.Parse(item.name));
                }
            }

            internal void SetActive(bool active)
            {
                this.activeSelf = active;
                if (selected != null)
                    selected.SetActive(active);
            }


            internal void Reset(string txt, Sprite sprite, bool active)
            {
                if (text != null)
                    m_Text = txt;
                if (image != null)
                    m_Image = sprite;
                SetActive(active);
            }
        }

        /// <summary>
        /// Cache Data
        /// </summary>
        [Serializable]
        public class GDropData
        {
            [SerializeField]
            private string m_Text;
            [SerializeField]
            private Sprite m_Image;
            public string text  { get { return m_Text; }  set { m_Text = value; } }
            public Sprite image { get { return m_Image; } set { m_Image = value; } }
            public GDropData(){}

                public GDropData(string text)
                {
                    this.text = text;
                }

                public GDropData(Sprite image)
                {
                    this.image = image;
                }

                public GDropData(string text, Sprite image)
                {
                    this.text = text;
                    this.image = image;
                }
        }

    }
}