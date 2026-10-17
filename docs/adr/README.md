# Arkitekturbeslut (ADR)

Varje arkitekturbeslut är en egen fil: `NNNN-kort-titel.md`, numrerade i följd och aldrig omnumrerade.

Ett nytt eller ändrat ADR godkänns av användaren via sin pull request (kontrollpunkt 2 i [agentsystem.md](../agentsystem.md)). Ett beslut som ersätts ändras inte; det får status "Ersatt av NNNN" och ett nytt ADR skrivs.

## Mall

```markdown
# NNNN. Titel

- Status: Föreslagen | Godkänd | Ersatt av NNNN
- Datum: ÅÅÅÅ-MM-DD

## Sammanhang
Vilket problem eller krav som tvingar fram beslutet.

## Beslut
Vad vi gör.

## Alternativ
Vad som övervägdes och varför det valdes bort.

## Konsekvenser
Vad som blir lättare, vad som blir svårare, och vilka GDPR-krav som berörs.
```
