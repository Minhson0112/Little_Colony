# Action notifications

Failed building purchases now show a modal with the required balance, current balance, and shortage. The shop retains its tab and scroll position. Food restrictions also show a modal.

Rejected work assignments show their validation error. When energy is exhausted, the popup explains how to buy and place food. English and Vietnamese strings live in the shared I18n catalog.

The modal blocks world input, shop scrolling, and underlying buttons. Its 48-unit confirmation button uses browser pixel density for mobile sizing. Escape and Enter dismiss it; input remains blocked for the dismissal frame.

Validation: 2,859 domain/localization checks; Unity Play Mode rejected-purchase and exhausted-work checks; WebGL build; browser at 844 × 390 verified a 300-acorn purchase with 210 available, retained shop context, and blocked clicks behind the popup. The popup does not change economy rules or saved data.
