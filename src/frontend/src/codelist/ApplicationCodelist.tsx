import { CodelistValue } from '../client';
import { ApplicationType, ApplicationTheme } from '../cms';

export const applicationTypeCodeList: CodelistValue[] = [
    {
        id: ApplicationType.MOBILE_APPLICATION,
        label: 'mobilná aplikácia'
    },
    {
        id: ApplicationType.WEB_APPLICATION,
        label: 'webová aplikácia'
    },
    {
        id: ApplicationType.WEB_PORTAL,
        label: 'webový portál'
    },
    {
        id: ApplicationType.VISUALIZATION,
        label: 'vizualizácia'
    },
    {
        id: ApplicationType.ANALYSIS,
        label: 'analýza'
    }
];

export const applicationThemeCodeList: CodelistValue[] = [
    {
        id: ApplicationTheme.EDUCATION,
        label: 'školstvo'
    },
    {
        id: ApplicationTheme.HEALTH,
        label: 'zdravotníctvo'
    },
    {
        id: ApplicationTheme.ENVIRONMENT,
        label: 'životné prostredie'
    },
    {
        id: ApplicationTheme.TRANSPORT,
        label: 'doprava'
    },
    {
        id: ApplicationTheme.CULTURE,
        label: 'kultúra'
    },
    {
        id: ApplicationTheme.TOURISM,
        label: 'cestovný ruch'
    },
    {
        id: ApplicationTheme.ECONOMY,
        label: 'ekonomika'
    },
    {
        id: ApplicationTheme.SOCIAL,
        label: 'sociálne veci'
    },
    {
        id: ApplicationTheme.PUBLIC_ADMINISTRATION,
        label: 'verejná správa'
    },
    {
        id: ApplicationTheme.OTHER,
        label: 'ostatné'
    }
];
