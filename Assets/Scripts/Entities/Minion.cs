﻿using System.Collections.Generic;
using UnityEngine;

public class Minion : Character
{
    public MinionCard Card;
    public BuffManager Buffs;

    #region Constructor

    public Minion(MinionCard card)
    {
        Player = card.Player;
        Card = card;
        Card.Minion = this;

        CurrentAttack = card.CurrentAttack;
        BaseAttack = card.BaseAttack;

        CurrentHealth = card.CurrentHealth;
        MaxHealth = card.CurrentHealth;
        BaseHealth = card.BaseHealth;

        HasTaunt = card.HasTaunt;
        HasCharge = card.HasCharge;
        HasPoison = card.HasPoison;
        HasWindfury = card.HasWindfury;
        HasDivineShield = card.HasDivineShield;
        IsElusive = card.IsElusive;
        IsForgetful = card.IsForgetful;
        IsStealth = card.IsStealth;

        SpellPower = card.SpellPower;

        CurrentArmor = 0;

        Buffs = Card.Buffs;
    }

    #endregion

    #region Methods

    public override void Attack(Character target)
    {
        Debugger.LogMinion(this, "starting attack to " + target.GetName());

        // Removing stealth of the Minion
        IsStealth = false;

        // Checking if minion is Forgetful
        if (IsForgetful)
        {
            // Checking if there's more than 1 enemy (hero + minions)
            if (Player.Enemy.Minions.Count > 0)
            {
                // Random 50% chance
                if (RNG.RandomBool())
                {
                    // TODO : Play forgetful trigger animation

                    // Creating a list of possible targets
                    List<Character> possibleTargets = new List<Character>();

                    // Adding the enemy hero to the list
                    possibleTargets.Add(Player.Enemy.Hero);

                    // Adding all enemy minions to the list
                    foreach (Minion enemyMinion in Player.Enemy.Minions)
                    {
                        possibleTargets.Add(enemyMinion);
                    }

                    // Removing the current target from the possible targets list
                    possibleTargets.Remove(target);

                    // Selecting a target by random
                    int randomTarget = Random.Range(0, possibleTargets.Count);

                    // Setting the current target as the random target
                    target = possibleTargets[randomTarget];

                    Debugger.LogMinion(this, "switched target to " + target.TypeName() + " (forgetful)");
                }
            }
        }

        // Firing OnPreAttack events
        Buffs.OnPreAttack.OnNext(null);
        MinionPreAttackEvent minionPreAttackEvent = EventManager.Instance.OnMinionPreAttack(this, target);

        // Checking if the Attack was not cancelled
        if (minionPreAttackEvent.Status != PreStatus.Cancelled)
        {
            // Adding 1 to the current turn attacks counter
            CurrentTurnAttacks++;

            // Redefining target in case it changed when firing events
            target = minionPreAttackEvent.Target;

            // Getting both characters current attack
            int attackerAttack = CurrentAttack;
            int targetAttack = target.CurrentAttack;

            Debugger.LogMinion(this, "attacking " + target.GetName());

            if (target.IsHero())
            {
                target.Damage(this, attackerAttack);
            }
            else if (target.IsMinion())
            {
                Minion targetMinion = target.As<Minion>();
                
                // Checking if both minions are still alive
                if (this.IsAlive() && target.IsAlive())
                {
                    // Damaging both minions
                    this.Damage(target, targetAttack);
                    target.Damage(this, attackerAttack);

                    // Checking the death of both characters
                    this.CheckDeath();
                    target.CheckDeath();

                    // Checking for poison on both minions
                    if (this.HasPoison)
                    {
                        if (attackerAttack > 0)
                        {
                            Debugger.LogMinion(this, "killed by posion of " + targetMinion.GetName());

                            EventManager.Instance.OnMinionPoisoned(targetMinion, this);

                            Destroy();
                        }
                    }

                    if (targetMinion.HasPoison)
                    {
                        if (targetAttack > 0)
                        {
                            Debugger.LogMinion(targetMinion, "killed by posion of " + this.GetName());

                            EventManager.Instance.OnMinionPoisoned(this, targetMinion);

                            Destroy();
                        }
                    }
                }
            }

            // Firing OnAttacked events
            Buffs.OnAttacked.OnNext(null);
            EventManager.Instance.OnMinionAttacked(this, target);
        }

        Controller.UpdateSprites();
    }

    public override void Heal(int healAmount)
    {
        // Firing OnMinionPreHeal events
        MinionPreHealEvent minionPreHealEvent = EventManager.Instance.OnMinionPreHeal(this, healAmount);

        // TODO : Check if heal is transformed to damage and if so, call Damage instead

        Debugger.LogMinion(this, "healing for " + minionPreHealEvent.HealAmount);

        // Healing the minion
        CurrentHealth = Mathf.Min(CurrentHealth + minionPreHealEvent.HealAmount, MaxHealth);

        // Firing OnMinionHealed events
        EventManager.Instance.OnMinionHealed(this, minionPreHealEvent.HealAmount);

        // TODO : Heal animation
        // TODO : Show heal sprite + healed amount

        Controller.UpdateNumbers();
    }

    // TODO : Gotta make the BuffManager methods to be able to modify the damage amounts
    public override void Damage(Character attacker, int damageAmount)
    {
        // Creating the MinionPreDamageEvent
        MinionPreDamageEvent minionPreDamageEvent = new MinionPreDamageEvent()
        {
            Attacker = attacker,
            Minion = this,
            DamageAmount = damageAmount
        };

        // Firing the subscribed handlers to OnPreDamage
        Buffs.OnPreDamage.OnNext(minionPreDamageEvent);
        EventManager.Instance.OnMinionPreDamage(minionPreDamageEvent);

        Debugger.LogMinion(this, "receiving " + minionPreDamageEvent.DamageAmount + " damage by " + attacker.GetName());

        // Substracting the damage to the current health of the Minion
        CurrentHealth -= minionPreDamageEvent.DamageAmount;

        // TODO : Show health loss sprite + amount on token

        // Creating the MinionDamagedEvent
        MinionDamagedEvent minionDamagedEvent = new MinionDamagedEvent()
        {
            Attacker = attacker,
            Minion = this,
            DamageAmount = minionPreDamageEvent.DamageAmount
        };

        // Firing the subscribed handlers to OnDamage
        Buffs.OnDamaged.OnNext(minionDamagedEvent);
        EventManager.Instance.OnMinionDamaged(minionDamagedEvent);

        Controller.UpdateNumbers();
    }

    public override void CheckDeath()
    {
        if (IsAlive() == false)
        {
            Die();
        }
    }

    public void Die()
    {
        Debugger.LogMinion(this, "died");

        // TODO : Custom animations, sounds, etc ?

        // Firing OnMinionDied events
        Buffs.Deathrattle.OnNext(this);
        EventManager.Instance.OnMinionDied(this);

        Remove();

        // Adding the Minion to the list of dead Minions
        Player.DeadMinions.Add(Card);
    }

    public void Destroy()
    {
        Debugger.LogMinion(this, "destroyed");

        // TODO : Play destroy animation (dust and stuff)

        Remove();

        // Adding the Minion to the list of dead Minions
        Player.DeadMinions.Add(Card);
    }

    private void Remove()
    {
        Debugger.LogMinion(this, "removed");

        // Removing the minion from the player list of minions
        Player.Minions.Remove(this);

        // Removing the controller from the board and destroying it
        Player.BoardController.RemoveMinion(this);
        Controller.DestroyController();
    }

    public void AddBuff(BaseBuff buff)
    {
        // Adding the buff to the list
        Buffs.AllBuffs.Add(buff);

        // Firing OnAdded for that buff
        buff.OnAdded(this);

        // Updating controller numbers
        Controller.UpdateNumbers();
    }

    public void RemoveBuff(BaseBuff buff)
    {
        // Checking if the minion has the buff
        if (Buffs.AllBuffs.Contains(buff))
        {
            // Removing the buff from the list
            Buffs.AllBuffs.Remove(buff);

            // Firing OnRemoved for that buff
            buff.OnRemoved(this);

            // Updating controller numbers
            Controller.UpdateNumbers();
        }
    }

    public void ReturnToHand()
    {
        Debugger.LogMinion(this, "returns to Hand");

        // Removing the minion from the board
        Remove();

        // Adding the card to the Hand
        Player.AddCardToHand(Card);
    }

    public void ReturnToDeck()
    {
        Debugger.LogMinion(this, "returns to Deck");

        // Removing the minion from the board
        Remove();

        // Adding the card to the Deck
        Player.AddCardToDeck(Card);
    }

    public void Silence()
    {
        Debugger.LogMinion(this, "silenced");

        // TODO : Play animations + sound and add silenced sprite

        Buffs.RemoveAll();

        HasTaunt = false;
        HasCharge = false;
        HasPoison = false;
        HasWindfury = false;
        HasDivineShield = false;
        IsImmune = false;
        IsElusive = false;
        IsStealth = false;

        SpellPower = 0;

        IsFrozen = false;
        UnfreezeNextTurn = false;
        IsForgetful = false;

        IsSilenced = true;
    }

    public void TransformInto(Minion other)
    {
        // TODO : Play transform animation

        // TODO : Transform minion without triggering anything, destroy old minion
    }

    #endregion

    #region Getter Methods
    


    #endregion

    #region Condition Checkers

    public override bool IsMinion()
    {
        return true;
    }

    public override bool CanAttack()
    {
        if (IsFrozen == false)
        {
            if (IsSleeping == false || HasCharge)
            {
                if (HasWindfury)
                {
                    return CurrentTurnAttacks < 2;
                }
                else
                {
                    return CurrentTurnAttacks < 1;
                }
            }
        }
        return false;
    }

    #endregion
}