using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UserInterface
{
    public class MainMenu : MonoBehaviour
    {
        private VisualElement _root;
        private Button _connectButton;
        private Button _exitButton;
        private DropdownField _serverIpDropdownField;
        public InputActionReference menuAction;

        void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _connectButton = _root.Q<Button>("ConnectButton");
            _connectButton.clicked += OnConnectButtonClicked;
            _exitButton = _root.Q<Button>("ExitButton");
            _exitButton.clicked += OnExitButtonClicked;
            _serverIpDropdownField = _root.Q<DropdownField>("ServerIpDropDown");
            _serverIpDropdownField.choices.Clear();
            _serverIpDropdownField.choices.AddRange(
                GameManager.ServerChoices.Select(sc => sc.Key)
            );
            
            GameManager.OnConnected += GameManagerOnOnConnected;
            GameManager.OnDisconnected += GameManagerOnOnDisconnected;
            menuAction.action.performed += ToggleMenu;
        }

        private void GameManagerOnOnConnected()
        {
            _connectButton.SetEnabled(false);
            _serverIpDropdownField.SetEnabled(false);
            _root.style.display = DisplayStyle.None;
        }
        
        private void GameManagerOnOnDisconnected()
        {
            _connectButton.SetEnabled(true);
            _serverIpDropdownField.SetEnabled(true);
            _root.style.display = DisplayStyle.Flex;
        }

        private void ToggleMenu(InputAction.CallbackContext ctx)
        {
            if(!GameManager.IsConnected()) OnExitButtonClicked();
            _root.style.display =
                _root.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnConnectButtonClicked()
        {
            var selectedServerIp = _serverIpDropdownField.value;
            GameManager.Instance.Connect(selectedServerIp);
        }

        private void OnExitButtonClicked()
        {
            Application.Quit();
        }
    }
}
