# Webshop API

Shop on https://shop.apex-performance.fit (test: https://test.shop.apex-performance.fit), administration in the
web app under Webshop. Products have variants (size and color) with their own stock. Customers pay by card in
Stripe Checkout and get the parcel in a BoxNow locker; eRačuni reads the payments from Stripe and emails the
fiscalized receipt.

## Settings

| Key | Default | Notes |
|---|---|---|
| `Stripe:ApexPerformance:SecretKey` | – | Required for checkout. Test keys on test, live keys of the Apex Performance Stripe account on production. |
| `Stripe:ApexPerformance:WebhookSecret` | – | Signing secret (`whsec_...`) of the webhook endpoint added in the Stripe dashboard. Without it webhooks are refused with 503 and only the 5-minute check confirms payments. |
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
`GET /api/shop/admin/orders`, `PUT /api/shop/admin/orders/{id}/status`,
`GET|POST /api/shop/admin/promo-codes`, `PUT|DELETE /api/shop/admin/promo-codes/{id}`,
`GET /api/shop/admin/stock`, `POST /api/shop/admin/stock/{variantId}`, `GET /api/shop/admin/stock/{variantId}/movements`.

Public promo check (rate limited): `POST /api/shop/promo-codes/check` with `{ code, items }`.

## Promo codes

A percent or an amount off each piece of the chosen products (all products when none is chosen), with optional
dates and a limit of paid orders. The cart checks a code with `/promo-codes/check`; the checkout checks it again
and sends Stripe the discounted price of each piece, so the eRačuni receipt shows what was paid. Free delivery
counts the products after the discount. Errors: `1304` unknown, inactive, expired or used up; `1305` not for any
product in the cart. A code used by an order can't be renamed.

## Warehouse

Every change of a variant's stock is a `ShopStockMovements` row: `Initial` (new variant), `Receipt` (goods received),
`Correction` (counted, or changed in the product form), `WriteOff`, `Sale` (paid order) and `Return` (paid order
cancelled).

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

## Stripe webhooks

Endpoint `POST /api/shop/stripe/webhook` (test: `https://test.apex-performance.fit/api/shop/stripe/webhook`), added
in Stripe under Developers → Webhooks with these events:

| Event | What happens |
|---|---|
| `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.expired` | The order is confirmed (or cancelled) right away, the same way as when the customer returns. |
| `charge.refunded` | `RefundedAmount` is saved. A full refund marks the order `Refunded`; an order not shipped yet gets its pieces back in stock. The shop gets an email. |
| `charge.dispute.created`, `charge.dispute.updated`, `charge.dispute.closed` | `DisputeStatus` is saved, the shop gets an email when the dispute opens (with the deadline) and when it closes. |
| `radar.early_fraud_warning.created` | `HasFraudWarning` is set and the shop gets an email not to ship. |

Every request's signature is checked; each event is handled once (`ShopStripeEvents`). Events of payments that are
not shop orders (other sites on the same Stripe account) are ignored.

## Accounts, client discount and settings

- `POST /api/shop/login` `{ username, password }` (username or email of the app account) returns a token. It opens a
  session of its own (`UserSessions.Channel = "Shop"`): the app's login doesn't end it and it doesn't end the app's.
  `GET /api/shop/me` returns the name, email, whether the user is a client and the client discount;
  `GET /api/shop/my-orders` lists their paid orders. Log out with `POST /api/authentication/logout`.
- The checkout reads the token when one is sent: a client of the studio gets `ClientDiscountPercent` (set in the
  administration) off every product. Per product the bigger of it and a promo code counts, they don't add up.
- `GET|PUT /api/shop/admin/settings`: delivery price, free delivery from, client discount, email for notifications.
  Empty values use the server settings above.

## Notifications

- Marking an order shipped emails the customer once (with the BoxNow parcel number).
- `POST /api/shop/stock-alerts` `{ variantId, email }`: one email when the size/color is back in stock (a minute
  after the stock change). Requests are deleted after 180 days.

## Sharing, search and visits

- `GET /api/shop/share/{slug}`: page with Open Graph tags for link previews that sends people on to the product.
- `GET /api/shop/sitemap.xml`, listed in the production shop's robots.txt.
- `POST /api/shop/visits` `{ path }`: counts a page view per day and page, nothing about the visitor (no cookies).
  `GET /api/shop/admin/stats?days=30` returns visits and sales per day and per product.
