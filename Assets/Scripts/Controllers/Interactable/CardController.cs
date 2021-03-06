﻿using UnityEngine;

public class CardController : BaseController
{
    public BaseCard Card;

    public int TargetRenderingOrder = 0;
    public Vector3 TargetPosition = Vector3.zero;
    public Vector3 TargetRotation = Vector3.zero;

    private SpriteRenderer CardRenderer;
    private SpriteRenderer ComboGlowRenderer;

    private NumberController CostController;
    private NumberController AttackController;
    private NumberController AttributeController;

    private BoxCollider CardCollider;

    private string GlowType;

    public static CardController Create(BaseCard card)
    {
        GameObject cardObject = new GameObject(card.Name);
        
        BoxCollider cardCollider = cardObject.AddComponent<BoxCollider>();
        cardCollider.size = new Vector3(3.5f, 5.5f, 1f);

        CardController controller = cardObject.AddComponent<CardController>();
        controller.Card = card;
        controller.CardCollider = cardCollider;
        controller.GlowType = controller.GetGlowType();

        controller.Initialize();

        return controller;
    }
    
    public override void Initialize()
    {
        CostController = NumberController.Create("Cost_Controller", this.gameObject, new Vector3(-1.375f, 2.15f, 0f), 43, 0.5f);
        AttackController = NumberController.Create("Attack_Controller", this.gameObject, new Vector3(-1.35f, -2.15f, 0f), 43, 0.5f);
        AttributeController = NumberController.Create("Attribute_Controller", this.gameObject, new Vector3(1.5f, -2.15f, 0f), 43, 0.5f);

        CardRenderer = CreateRenderer("Card_Sprite", Vector3.one, Vector3.zero, 42);

        switch (Card.GetCardType())
        {
            case CardType.Weapon:
                ComboGlowRenderer = CreateRenderer("ComboGlow_Sprite", Vector3.one * 2.5f, new Vector3(0.0375f, 0.025f, 0f), 41);
                GreenGlowRenderer = CreateRenderer("GreenGlow_Sprite", Vector3.one * 2.5f, new Vector3(0.0375f, 0.025f, 0f), 40);
                break;

            case CardType.Spell:
                ComboGlowRenderer = CreateRenderer("ComboGlow_Sprite", Vector3.one * 2.5f, Vector3.zero, 41);
                GreenGlowRenderer = CreateRenderer("GreenGlow_Sprite", Vector3.one * 2.5f, Vector3.zero, 40);
                break;

            case CardType.Minion:
                ComboGlowRenderer = CreateRenderer("ComboGlow_Sprite", Vector3.one * 2.5f, new Vector3(0.07f, 0f, 0f), 41);
                GreenGlowRenderer = CreateRenderer("GreenGlow_Sprite", Vector3.one * 2.5f, new Vector3(0.07f, 0f, 0f), 40);
                break;
        }

        CardRenderer.enabled = true;

        UpdateSprites();
        UpdateNumbers();
    }

    public override void DestroyController()
    {
        Destroy(CardRenderer);
        Destroy(GreenGlowRenderer);
        Destroy(ComboGlowRenderer);

        Destroy(this.gameObject);
    }

    public override void UpdateSprites()
    {
        // Loading the sprites into the SpriteRenderers
        CardRenderer.sprite = Resources.Load<Sprite>("Sprites/" + Card.Class.Name() + "/Cards/" + Card.TypeName());
        GreenGlowRenderer.sprite = SpriteManager.Instance.Glows["Card_" + GlowType + "_GreenGlow"];
        //ComboGlowRenderer.sprite = SpriteManager.Instance.Glows["Card_" + GlowType + "_ComboGlow"];
    }

    public override void UpdateNumbers()
    {
        CostController.UpdateNumber(Card.CurrentCost, Util.GetColor(Card.CurrentCost, Card.BaseCost));

        switch (Card.GetCardType())
        {
            case CardType.Minion:
                MinionCard minionCard = Card.As<MinionCard>();
                AttackController.UpdateNumber(minionCard.CurrentAttack, Util.GetColor(minionCard.CurrentAttack, minionCard.BaseAttack));
                AttributeController.UpdateNumber(minionCard.CurrentHealth, Util.GetColor(minionCard.CurrentHealth, minionCard.BaseHealth));
                break;

            case CardType.Weapon:
                WeaponCard weaponCard = Card.As<WeaponCard>();
                AttackController.UpdateNumber(weaponCard.CurrentAttack, Util.GetColor(weaponCard.CurrentAttack, weaponCard.BaseAttack));
                AttributeController.UpdateNumber(weaponCard.CurrentDurability, Util.GetColor(weaponCard.CurrentDurability, weaponCard.BaseDurability));
                break;
        }
    }

    public void SetRenderingOrder(int order)
    {
        CostController.SetRenderingOrder(order + 3);
        AttackController.SetRenderingOrder(order + 3);
        AttributeController.SetRenderingOrder(order + 3);

        CardRenderer.sortingOrder = order + 2;

        ComboGlowRenderer.sortingOrder = order + 1;
        GreenGlowRenderer.sortingOrder = order;
    }

    private string GetGlowType()
    {
        switch (Card.GetType().BaseType.Name)
        {
            case "MinionCard":
                if (Card.As<MinionCard>().Rarity == CardRarity.Legendary)
                {
                    return "LegendaryMinion";
                }
                return "Weapon";

            case "SpellCard":
                return "Spell";

            case "WeaponCard":
                return "Weapon";

            default:
                return "Normal";
        }
    }

    #region Unity Messages

    private ControllerStatus Status = ControllerStatus.Inactive;
    private bool IsHovering = false;

    private void Update()
    {
        float moveSpeed = 100f * Time.deltaTime;

        Vector3 targetZoomPosition = new Vector3(TargetPosition.x, 13f, 0f);

        switch (Status)
        {
            case ControllerStatus.Inactive:
                if (IsHovering && transform.localPosition.x == TargetPosition.x && InterfaceManager.Instance.IsTargeting == false && InterfaceManager.Instance.IsDragging == false)
                {
                    SetRenderingOrder(500);
                    transform.localScale = Vector3.one * 2f;
                    transform.localEulerAngles = Vector3.zero;
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetZoomPosition, moveSpeed);
                }
                else
                {
                    SetRenderingOrder(TargetRenderingOrder);
                    transform.localScale = Vector3.one;
                    transform.localEulerAngles = TargetRotation;
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, moveSpeed);
                }
                break;

            case ControllerStatus.Dragging:
                SetRenderingOrder(500);
                transform.localScale = Vector3.one;
                transform.localEulerAngles = Vector3.zero;
                transform.position = Util.GetWorldMousePosition();
                break;

            case ControllerStatus.Targeting:
                SetRenderingOrder(500);
                transform.localScale = Vector3.one;
                transform.localEulerAngles = Vector3.zero;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetZoomPosition, moveSpeed);
                break;
        }
    }

    private void OnMouseEnter()
    {
        IsHovering = true;
    }

    private void OnMouseExit()
    {
        IsHovering = false;
    }

    private void OnMouseDown()
    {
        switch (Card.GetCardType())
        {
            case CardType.Minion: // TODO: if is frozen u can't do anything with it
            case CardType.Weapon:
                Status = ControllerStatus.Dragging;

                InterfaceManager.Instance.IsDragging = true;
                break;

            case CardType.Spell:
                if (Card.As<SpellCard>().TargetType == TargetType.NoTarget)
                {
                    Status = ControllerStatus.Dragging;
                    InterfaceManager.Instance.IsDragging = true;
                }
                else
                {
                    Status = ControllerStatus.Targeting;
                    InterfaceManager.Instance.EnableArrow(this);
                }
                break;
        }
    }

    private void OnMouseUp()
    {
        // Changing the status of the Card
        Status = ControllerStatus.Inactive;

        InterfaceManager.Instance.DisableArrow();
        InterfaceManager.Instance.IsDragging = false;

        // Checking if it's the turn of the Player
        if (GameManager.Instance.CurrentPlayer == Card.Player)
        {
            // Checking if the Player has enough mana to play the Card
            if (Card.Player.AvailableMana >= Card.CurrentCost)
            {
                switch (Card.GetCardType())
                {
                    case CardType.Minion:
                        if (Card.Player.BoardController.SelfBoardContainsPoint(Util.GetWorldMousePosition()))
                        {
                            if (Card.Player.Minions.Count < 7)
                            {
                                Card.Play();
                            }
                        }
                        break;

                    case CardType.Weapon:
                        if (Card.Player.BoardController.AllBoardContainsPoint(Util.GetWorldMousePosition()))
                        {
                            Card.Play();
                        }
                        break;

                    case CardType.Spell:
                        SpellCard spellCard = Card.As<SpellCard>();

                        if (spellCard.TargetType == TargetType.NoTarget)
                        {
                            if (Card.Player.BoardController.AllBoardContainsPoint(Util.GetWorldMousePosition()))
                            {
                                spellCard.PlayOn(null);
                            }
                        }
                        else
                        {
                            Character target = Util.GetCharacterAtMouse();

                            if (spellCard.CanTarget(target))
                            {
                                spellCard.PlayOn(target);
                            }
                        }
                        break;
                }
            }
            else
            {
                // TODO : Display not enough mana message
            }
        }
    }

    #endregion
}