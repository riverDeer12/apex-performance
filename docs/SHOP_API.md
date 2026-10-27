# Webshop API

Shop on https://shop.apex-performance.fit (test: https://test.shop.apex-performance.fit), administration in the
web app under Webshop. Products have variants (size and color) with their own stock. Customers pay by card in
Stripe Checkout and get the parcel in a BoxNow locker; eRačuni reads the payments from Stripe and emails the
fiscalized receipt.

## Settings

| Key | Default | Notes |
|---|---|---|
| `Stripe:ApexPerformance:SecretKey` | – | Required for checkout. Test keys on test, live keys of the Apex Performance Stripe account on production. |
| `Shop:Url` | `https://shop.apex-performance.fit` / `https://test.shop.apex-performance.fit` | Stripe returns the customer here. |
| `Shop:ShippingPrice` | `3.50` | BoxNow delivery in EUR. |
| `Shop:FreeShippingFrom` | `50` | Free delivery from this subtotal on. |
| `Shop:NotificationEmail` | `gabriel@apex-performance.fit` (production), `info@rdd.software` (elsewhere) | New orders and BoxNow labels. |
| `BoxNow:ApexPerformance:{ApiUrl, ClientId, ClientSecret, WarehouseId, ContactNumber, ContactEmail, ContactName, PartnerId}` | – | Outside production the `BoxNow:ReVivPlus` values (and partner id 11406) are used while these are missing. Production never borrows them. |

## Endpoints

Public: `GET /api/shop/products`, `GET /api/shop/products/{slug}`, `GET /api/shop/images/{id}`,
`GET /api/shop/settings`, `POST /api/shop/checkout` (rate limited), `GET /api/shop/orders/{id}?sessionId=...`.

Administrators: `GET|POST /api/shop/admin/products`, `PUT|DELETE /api/shop/admin/products/{id}`,
`POST|PUT /api/shop/admin/products/{id}/images`, `DELETE /api/shop/admin/products/{id}/images/{imageId}`,
`GET /api/shop/admin/orders`, `PUT /api/shop/admin/orders/{id}/status`.

## Order flow

1. `POST /checkout` takes prices and stock from the database, saves a `Pending` order and returns the Stripe
   Checkout address (valid 1 hour).
2. After paying, Stripe sends the customer to `/narudzba/{id}?session_id=...`; `GET /orders/{id}` checks the
   payment with Stripe. `ShopOrderSyncJob` (every 5 minutes) does the same for customers who closed the browser
   and cancels expired checkouts.
3. The first confirmation marks the order `Paid`, lowers the stock (`HasStockIssue` when two customers bought the
   last piece at once), emails the customer and the shop, and creates the BoxNow parcel; its label is emailed to
   the shop.
4. The shop marks the order `Shipped` in the administration. Cancelling a paid order returns the pieces to stock;
   the refund is made in Stripe.
