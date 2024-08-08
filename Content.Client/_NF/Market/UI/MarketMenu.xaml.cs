﻿using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.BUI;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Market.UI;

[GenerateTypedNameReferences]
public sealed partial class MarketMenu : FancyWindow
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public event Action<BaseButton.ButtonEventArgs>? OnAddToCart1;
    public event Action<BaseButton.ButtonEventArgs>? OnAddToCart5;
    public event Action<BaseButton.ButtonEventArgs>? OnAddToCart10;
    public event Action<BaseButton.ButtonEventArgs>? OnAddToCart30;
    public event Action<BaseButton.ButtonEventArgs>? OnAddToCartAll;
    public event Action<BaseButton.ButtonEventArgs>? OnReturn;
    public event Action<BaseButton.ButtonEventArgs>? OnPurchaseCart;

    private MarketConsoleInterfaceState? _lastStateUpdate;
    private string _searchText = "";

    public MarketMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        PurchaseCart.OnPressed += args => OnPurchaseCart?.Invoke(args);
        SearchText.OnTextChanged += args => OnSearchTextChanged(args.Text);
        ClearSearchButton.OnPressed += ClearSearchText;
    }

    private void ClearSearchText(BaseButton.ButtonEventArgs args)
    {
        _searchText = "";
        SearchText.Text = "";
        if (_lastStateUpdate != null)
        {
            UpdateState(_lastStateUpdate);
        }
    }

    public void SetUiEnabled(bool enabled)
    {
        SearchText.Editable = enabled;
        PurchaseCart.Disabled = !enabled;
    }

    private void OnSearchTextChanged(string text)
    {
        _searchText = text;
        if (_lastStateUpdate != null)
        {
            UpdateState(_lastStateUpdate);
        }
    }

    public void UpdateState(MarketConsoleInterfaceState uiState)
    {
        _lastStateUpdate = uiState;
        Populate(uiState.MarketDataList, uiState.CartDataList, uiState.MarketModifier, uiState.Enabled);
        CartEntitiesCount.Text = $"{uiState.CartEntities}/30";
        if (uiState.CartEntities == 30)
            CartEntitiesCount.FontColorOverride = Color.OrangeRed;
        else
            CartEntitiesCount.FontColorOverride = null;
        BalanceLabel.Text = $" ${uiState.Balance}";
        CartBalanceLabel.Text = _loc.GetString("market-cart-balance", ("cost", uiState.CartBalance), ("cratecost", uiState.TransactionCost));
        PurchaseCart.Text = _loc.GetString("market-purchase-cart-button") +
                            (uiState.CartBalance + uiState.TransactionCost);
        SetUiEnabled(uiState.Enabled);
        PurchaseCart.Disabled = uiState.CartDataList.Count <= 0;
    }

    private void Populate(List<MarketData> data, List<MarketData> cartData, float marketModifier, bool enabled = true)
    {
        Products.RemoveAllChildren();
        Cart.RemoveAllChildren();

        AddRows(Products, false, data, marketModifier, enabled);
        AddRows(Cart, true, cartData, marketModifier, enabled);
    }

    private void AddRows(Control container, bool isCart, List<MarketData> data, float marketModifier, bool enabled = true)
    {
        foreach (var marketData in data.OrderBy(md => md.Prototype))
        {
            // Try to get the EntityPrototype that matches marketData.Prototype
            if (!_protoManager.TryIndex<EntityPrototype>(marketData.Prototype, out var prototype))
            {
                continue; // Skip this iteration if the prototype was not found
            }

            if (!IsWithinSearchQuery(prototype))
                continue;

            if (!prototype.TryGetComponent<SpriteComponent>(out var sprite))
            {
                continue; // Skip this iteration if the prototype was not found
            }

            if (isCart)
            {
                var productRow = new MarketCartProductRow(prototype)
                {
                    Title = { Text = prototype.Name },
                    Quantity = { Text = marketData.Quantity.ToString() },
                    Price = { Text = $"${(int) double.Round(marketData.Quantity * marketData.Price * marketModifier)}" },
                    Icon = { Texture = sprite.Icon?.Default }
                };
                productRow.Return.OnPressed += args => { OnReturn?.Invoke(args); };
                productRow.Return.Disabled = !enabled;

                container.AddChild(productRow);
            }
            else
            {
                var priceText = $"${(int) double.Round(marketData.Quantity * marketData.Price *marketModifier)}";
                if (marketData.Quantity > 1)
                {
                    priceText += " ($" + (int) double.Round(marketData.Price * marketModifier) + " ea)";
                }

                var productRow = new MarketProductRow(prototype)
                {
                    Title = { Text = prototype.Name },
                    Quantity = { Text = _loc.GetString("market-quantity-available")
                        .Replace("$1", marketData.Quantity.ToString()) },
                    Price = { Text = priceText },
                    Icon = { Texture = sprite.Icon?.Default }
                };
                productRow.AddToCart1.OnPressed += args => { OnAddToCart1?.Invoke(args); };
                productRow.AddToCart1.Disabled = !enabled;

                if (marketData.Quantity > 1)
                {
                    productRow.AddToCartAll.Visible = true;
                    productRow.AddToCartAll.Disabled = !enabled;
                    productRow.AddToCartAll.OnPressed += args => { OnAddToCartAll?.Invoke(args); };
                }
                else
                    productRow.AddToCartAll.Visible = false;

                if (marketData.Quantity >= 5)
                {
                    productRow.AddToCart5.Visible = true;
                    productRow.AddToCart5.Disabled = !enabled;
                    productRow.AddToCart5.OnPressed += args => { OnAddToCart5?.Invoke(args); };
                }
                else
                    productRow.AddToCart5.Visible = false;

                if (marketData.Quantity >= 10)
                {
                    productRow.AddToCart10.Visible = true;
                    productRow.AddToCart10.Disabled = !enabled;
                    productRow.AddToCart10.OnPressed += args => { OnAddToCart10?.Invoke(args); };
                }
                else
                    productRow.AddToCart10.Visible = false;

                if (marketData.Quantity >= 30)
                {
                    productRow.AddToCart30.Visible = true;
                    productRow.AddToCart30.Disabled = !enabled;
                    productRow.AddToCart30.OnPressed += args => { OnAddToCart30?.Invoke(args); };
                }
                else
                    productRow.AddToCart30.Visible = false;


                container.AddChild(productRow);
            }
        }
    }

    private bool IsWithinSearchQuery(EntityPrototype prototype)
    {
        var text = _searchText.Trim();
        return string.IsNullOrEmpty(text) || prototype.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase);
    }
}
