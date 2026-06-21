# CLICK payment integration

EnglishAI uses CLICK SHOP API for subscription payments.

## Merchant cabinet URLs

Configure the active CLICK service with these production callbacks:

- Prepare URL: `https://englishai.uz/api/subscription/payments/click/prepare`
- Complete URL: `https://englishai.uz/api/subscription/payments/click/complete`

Do not save these URLs in the merchant cabinet until the matching backend release and database migration are deployed.

## Required production configuration

Set secrets through deployment environment variables, not `appsettings.json`:

```text
Subscription__PaymentsEnabled=true
Payments__MockMode=false
Payments__PublicBaseUrl=https://englishai.uz
Payments__WebhookBaseUrl=https://englishai.uz
Payments__Click__MerchantId=<CLICK merchant id>
Payments__Click__MerchantUserId=<optional merchant user id>
Payments__Click__ServiceId=<CLICK service id>
Payments__Click__SecretKey=<CLICK secret key>
Payments__Click__CheckoutUrl=https://my.click.uz/services/pay/
Payments__Click__BaseUrl=https://api.click.uz/v2/merchant
```

`SecretKey` must never be exposed to the frontend, logs, screenshots, or source control.

## Protocol

The checkout URL uses the official CLICK payment-link parameters: `merchant_id`, `service_id`, `transaction_param`, `amount`, and `return_url`.

CLICK calls both callbacks using `POST` and `application/x-www-form-urlencoded`. The backend verifies the official MD5 formulas before processing:

- Prepare: `md5(click_trans_id + service_id + SECRET_KEY + merchant_trans_id + amount + action + sign_time)`
- Complete: `md5(click_trans_id + service_id + SECRET_KEY + merchant_trans_id + merchant_prepare_id + amount + action + sign_time)`

The return URL is only browser navigation. Premium access is activated only by a valid successful Complete callback.

## Deployment order

1. Back up the database.
2. Deploy the backend release.
3. Apply `AddClickPaymentCallbackState` migration.
4. Set the environment variables above.
5. Restart the API and verify readiness.
6. Enter Prepare and Complete URLs in the CLICK merchant cabinet.
7. Ask CLICK to activate or confirm the service callbacks.
8. Run a small real payment and verify callback logs, payment status, and Premium activation.

CLICK may require the production domain, static IP address, and port for firewall allowlisting before the first payment.
