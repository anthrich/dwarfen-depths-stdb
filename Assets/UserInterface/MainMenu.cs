using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UserInterface
{
    public class MainMenu : MonoBehaviour
    {
        private VisualElement _root;
        private Button _connectButton;
        private DropdownField _serverIpDropdownField;

        void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _connectButton = _root.Q<Button>("ConnectButton");
            _connectButton.clicked += OnButtonClicked;
            _serverIpDropdownField = _root.Q<DropdownField>("ServerIpDropDown");
            _serverIpDropdownField.choices.Clear();
            _serverIpDropdownField.choices.AddRange(
                GameManager.ServerChoices.Select(sc => sc.Key)
            );
            
            GameManager.OnConnected += GameManagerOnOnConnected;
        }

        private void GameManagerOnOnConnected()
        {
            gameObject.SetActive(false);
        }

        private void OnButtonClicked()
        {
            var selectedServerIp = _serverIpDropdownField.value;
            GameManager.Instance.Connect(selectedServerIp);
        }
    }
}
