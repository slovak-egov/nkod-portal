import { PropsWithChildren, createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';

export const cookiesPolicyCookieName = 'cookies_policy';
const cookiesPolicyMaxAgeDays = 365;

export const cookiesProcessingPath = '/spracuvanie-cookies';
export const cookiesSettingsPath = '/cookies';

export type CookiesPolicy = {
    essential: true;
    analytics: boolean;
};

export type CookiesConfirmation = 'accepted' | 'rejected' | null;

type CookieConsentContextValue = {
    policy: CookiesPolicy | null;
    confirmation: CookiesConfirmation;
    savePolicy: (analytics: boolean, showConfirmation: boolean) => void;
    hideConfirmation: () => void;
};

const CookieConsentContext = createContext<CookieConsentContextValue | null>(null);

function readCookie(name: string): string | null {
    const prefix = name + '=';
    const item = document.cookie
        .split(';')
        .map((c) => c.trim())
        .find((c) => c.startsWith(prefix));
    return item ? decodeURIComponent(item.substring(prefix.length)) : null;
}

export function readCookiesPolicy(): CookiesPolicy | null {
    const value = readCookie(cookiesPolicyCookieName);
    if (!value) {
        return null;
    }

    try {
        const parsed = JSON.parse(value);
        if (parsed && typeof parsed.analytics === 'boolean') {
            return { essential: true, analytics: parsed.analytics };
        }
    } catch {}

    return null;
}

function writeCookiesPolicy(policy: CookiesPolicy) {
    const expires = new Date();
    expires.setDate(expires.getDate() + cookiesPolicyMaxAgeDays);
    const secure = window.location.protocol === 'https:' ? '; Secure' : '';
    document.cookie =
        cookiesPolicyCookieName + '=' + encodeURIComponent(JSON.stringify(policy)) + '; expires=' + expires.toUTCString() + '; path=/; SameSite=Lax' + secure;
}

const matomoUrl = process.env.REACT_APP_MATOMO_URL;
let matomoLoaded = false;

function getMatomoTrackers(): any[] {
    try {
        return (window as any).Matomo?.getAsyncTrackers?.() ?? [];
    } catch {
        return [];
    }
}

function enableAnalytics() {
    if (!matomoUrl) {
        return;
    }

    if (matomoLoaded) {
        // Skript je už načítaný (súhlas bol v tejto relácii odvolaný a znovu udelený)
        getMatomoTrackers().forEach((t) => {
            try {
                t.setConsentGiven?.();
            } catch {}
        });
        return;
    }

    matomoLoaded = true;
    const w = window as any;
    const mtm = (w._mtm = w._mtm || []);
    mtm.push({ 'mtm.startTime': new Date().getTime(), event: 'mtm.Start' });
    const script = document.createElement('script');
    script.async = true;
    script.src = matomoUrl;
    const firstScript = document.getElementsByTagName('script')[0];
    if (firstScript?.parentNode) {
        firstScript.parentNode.insertBefore(script, firstScript);
    } else {
        document.head.appendChild(script);
    }
}

function deleteAnalyticsCookies() {
    const names = document.cookie
        .split(';')
        .map((c) => c.trim().split('=')[0])
        .filter((n) => n.startsWith('_pk_') || n.startsWith('mtm_'));

    const hostParts = window.location.hostname.split('.');
    const domains: (string | null)[] = [null];
    for (let i = 0; i < hostParts.length - 1; i++) {
        domains.push(hostParts.slice(i).join('.'));
    }

    names.forEach((name) => {
        domains.forEach((domain) => {
            document.cookie = name + '=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/' + (domain ? '; domain=' + domain : '');
        });
    });
}

function disableAnalytics() {
    getMatomoTrackers().forEach((t) => {
        try {
            t.requireConsent?.();
            t.forgetConsentGiven?.();
        } catch {}
    });
    deleteAnalyticsCookies();
}

export function CookieConsentProvider(props: PropsWithChildren) {
    const [policy, setPolicy] = useState<CookiesPolicy | null>(() => readCookiesPolicy());
    const [confirmation, setConfirmation] = useState<CookiesConfirmation>(null);

    const analytics = policy?.analytics ?? false;

    useEffect(() => {
        if (analytics) {
            enableAnalytics();
        } else {
            disableAnalytics();
        }
    }, [analytics]);

    const savePolicy = useCallback((analytics: boolean, showConfirmation: boolean) => {
        const newPolicy: CookiesPolicy = { essential: true, analytics: analytics };
        writeCookiesPolicy(newPolicy);
        setPolicy(newPolicy);
        setConfirmation(showConfirmation ? (analytics ? 'accepted' : 'rejected') : null);
    }, []);

    const hideConfirmation = useCallback(() => setConfirmation(null), []);

    const value = useMemo(() => ({ policy, confirmation, savePolicy, hideConfirmation }), [policy, confirmation, savePolicy, hideConfirmation]);

    return <CookieConsentContext.Provider value={value}>{props.children}</CookieConsentContext.Provider>;
}

export function useCookieConsent() {
    const ctx = useContext(CookieConsentContext);
    if (ctx === null) {
        throw new Error('useCookieConsent must be used within CookieConsentProvider');
    }
    return ctx;
}
