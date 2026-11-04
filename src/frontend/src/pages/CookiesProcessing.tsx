import { PropsWithChildren, ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useDocumentTitle } from '../client';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import { ExternalLink, P } from '../components/LegalText';

const webUrl = 'https://data.slovensko.sk/';
const supportUrl = 'https://slovak-egov.atlassian.net/wiki/spaces/opendata/pages/20056044/Podpora+pre+data.slovensko.sk';

type CookieRow = {
    name: ReactNode;
    retention: string;
    purpose: string;
};

function CookiesTable(props: { caption: string; rows: CookieRow[] }) {
    return (
        <table className="govuk-table">
            <caption className="govuk-visually-hidden">{props.caption}</caption>
            <thead className="govuk-table__head">
                <tr className="govuk-table__row">
                    <th scope="col" className="govuk-table__header">
                        Názov cookies
                    </th>
                    <th scope="col" className="govuk-table__header">
                        Doba uchovávania
                        <br />
                        (od navštívenia webovej stránky)
                    </th>
                    <th scope="col" className="govuk-table__header">
                        Účel spracúvania
                    </th>
                </tr>
            </thead>
            <tbody className="govuk-table__body">
                {props.rows.map((r, i) => (
                    <tr className="govuk-table__row" key={i}>
                        <td className="govuk-table__cell">{r.name}</td>
                        <td className="govuk-table__cell">{r.retention}</td>
                        <td className="govuk-table__cell">{r.purpose}</td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
}

function Item(props: PropsWithChildren) {
    return <li className="govuk-!-margin-bottom-6">{props.children}</li>;
}

function LetterList(props: PropsWithChildren) {
    return (
        <ol className="govuk-list govuk-list--number" style={{ listStyleType: 'lower-alpha' }}>
            {props.children}
        </ol>
    );
}

const necessaryCookies: CookieRow[] = [
    {
        name: 'accessToken',
        retention: 'Po dobu trvania relácie',
        purpose: 'Slúži na uloženie Access Tokenu pre prihláseného používateľa.'
    }
];

const analyticsCookies: CookieRow[] = [
    {
        name: '_pk_id.1.fc9f (data.slovensko.sk)',
        retention: '13 mesiacov',
        purpose:
            'Obsahuje jedinečné ID návštevníka na rozpoznanie nových a vracajúcich sa návštevníkov s cieľom vytvoriť profil návštevníka, ktorý zahŕňa súhrn ich interakcií na stránke, počet návštev, časovú pečiatku prvej a poslednej návštevy.'
    },
    {
        name: '_pk_ses.1.fc9f (data.slovensko.sk)',
        retention: '30 minút',
        purpose:
            'Používa sa na prepojenie akcií vykonaných počas relácie (napr. zobrazenia stránok, stiahnutia, udalosti) s jedinečnou návštevou, čo umožňuje presne priradiť tieto akcie k jednej relácii.'
    },
    {
        name: (
            <>
                _pk_ref.1.fc9f
                <br />
                (data.slovensko.sk)
            </>
        ),
        retention: '6 mesiacov',
        purpose:
            'Údaje zahŕňajú spôsob, akým návštevník pôvodne prišiel na stránku, či už prišiel z vyhľadávača, odkazu na sociálne siete, externej webovej stránky alebo adresy URL. Tieto informácie následne pomáhajú priradiť návštevy ku konkrétnym zdrojom návštevnosti.'
    }
];

export default function CookiesProcessing() {
    const { t } = useTranslation();
    useDocumentTitle('Spracúvanie cookies');

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: 'Spracúvanie cookies' }]} />
            <MainContent>
                <PageHeader size="l">Spracúvanie cookies na webovej stránke Národného katalógu otvorených dát</PageHeader>

                <h2 className="govuk-heading-m">Informácie pre dotknutú osobu pri získaní osobných údajov</h2>
                <P>
                    Podľa čl. 13 a čl. 14 nariadenia Európskeho parlamentu a Rady (EÚ) č. 2016/679 z 27. apríla 2016 o ochrane fyzických osôb pri spracúvaní
                    osobných údajov a o voľnom pohybe takých údajov, ktorým sa zrušuje smernica 95/46/ES (všeobecné nariadenie o ochrane údajov) (ďalej len
                    „GDPR“) Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky (ďalej ako „ministerstvo”, ,,MIRRI SR“ alebo
                    „prevádzkovateľ”) ako subjekt, ktorý spracováva osobné údaje dotknutej osoby, poskytuje dotknutej osobe nasledovné informácie:
                </P>

                <ol className="govuk-list govuk-list--number">
                    <Item>
                        <P>
                            <strong>Identifikačné a kontaktné údaje prevádzkovateľa:</strong>
                        </P>
                        <P>
                            Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky
                            <br />
                            Pribinova 25, 811 09 Bratislava, IČO: 50349287
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Zodpovedná osoba pre oblasť osobných údajov MIRRI SR:</strong>
                        </P>
                        <ul className="govuk-list govuk-list--bullet">
                            <li>telefón: +421 2 2092 8425</li>
                            <li>
                                e-mail: <ExternalLink href="mailto:zodpovedna.osoba@mirri.gov.sk">zodpovedna.osoba@mirri.gov.sk</ExternalLink>
                            </li>
                        </ul>
                    </Item>

                    <Item>
                        <P>
                            <strong>Názov spracúvania</strong>: Spracúvanie cookies na webovej stránke <ExternalLink href={webUrl} />
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Čo sú to cookies?</strong>
                        </P>
                        <P>
                            Aby sme Vám mohli poskytnúť prístup k tejto webovej stránke a k službám prispôsobeným Vašim potrebám, potrebujeme poznať a uchovávať
                            informácie o tom, akým spôsobom používate túto webovú stránku.
                        </P>
                        <P>
                            To dosiahneme pomocou malých textových súborov, tzv. cookies. Cookies obsahujú malé množstvo informácií a sú sťahované
                            prostredníctvom serveru tejto webovej stránky do Vášho počítača, laptopu alebo iného zariadenia. Váš internetový prehliadač potom
                            zašle súbory cookies späť tejto webovej stránke pri každej ďalšej návšteve, vďaka čomu Vás táto webová stránka rozpozná a nájde
                            informácie napr. o vašich používateľských preferenciách (navštívené stránky, kliknutia, história aktivít).
                        </P>
                        <P>
                            Kedykoľvek navštívite túto webovú stránku, môže dochádzať k zhromažďovaniu informácií prostredníctvom súborov cookies alebo iných
                            technológií (informácie o jazyku, krajine a predtým nastavených stránkach).
                        </P>
                        <P>
                            Cookies sú odosielané našej webovej stránke (naše vlastné cookies) alebo sú odoslané inej webovej stránke, ktorá tieto cookies
                            spracúva (cookies tretích strán). V prípade, ak webovú stránku <ExternalLink href={webUrl} /> navštívite z iného zariadenia ako zo
                            zariadenia, na ktorom ste si už vlastné preferencie cookies nastavili alebo v prípade, ak nastane zmena v našom procese spracúvania
                            cookies alebo ak uplynie doba, po ktorú sme Vaše preferencie cookies uchovávali, opätovne Vás požiadame o nastavenie cookies na
                            našej webovej stránke.
                        </P>
                        <P>MIRRI SR aktuálne spracúva vlastné cookies aj cookies tretích strán.</P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Rozsah spracúvaných osobných údajov:</strong>
                        </P>
                        <P>
                            Z pohľadu GDPR sú niektoré cookies zároveň považované za osobné údaje. Cookies sú osobnými údajmi vtedy, ak je na základe cookies
                            možné identifikovať Vás ako konkrétnu osobu. Ministerstvo aktuálne spracúva nevyhnutné cookies a analytické cookies.
                        </P>
                        <P>
                            Ministerstvo spracúva nasledujúce <strong>nevyhnutné cookies:</strong>
                        </P>
                        <CookiesTable caption="Nevyhnutné cookies" rows={necessaryCookies} />
                        <P>
                            Ministerstvo spracúva nasledujúce <strong>analytické cookies:</strong>
                        </P>
                        <CookiesTable caption="Analytické cookies" rows={analyticsCookies} />
                        <P>
                            Pri prístupe na linku v päte webu <ExternalLink href={webUrl} />{' '}
                            <strong>
                                „<ExternalLink href={supportUrl}>Technická podpora</ExternalLink>“
                            </strong>{' '}
                            je používateľ presmerovaný na cloudovú službu Confluence spoločnosti Atlassian (
                            <em>
                                <ExternalLink href={supportUrl}>podpora pre data.slovensko.sk, metodika pre sprístupňovanie otvorených údajov</ExternalLink>
                            </em>
                            ). Atlassian spracúva nevyhnutné cookies, ktoré sú potrebné na zabezpečenie funkčnosti a bezpečnosti služby. Po udelení Vášho
                            súhlasu môže spoločnosť Atlassian spracúvať aj analytické a marketingové cookies. Viac informácií o spracúvaní cookies spoločnosťou
                            Atlassian je dostupných na nasledujúcich odkazoch: <ExternalLink href="https://www.atlassian.com/legal/privacy-policy" /> a{' '}
                            <ExternalLink href="https://www.atlassian.com/legal/cookies#cookies-and-other-tracking-technologies" />.
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Kategórie osobných údajov:</strong>
                        </P>
                        <P>
                            Ministerstvo spracúva bežné osobné údaje. Ministerstvo nespracúva osobné údaje osobitnej kategórie, údaje o trestných činoch a
                            priestupkoch.
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Kategórie dotknutých osôb:</strong>
                        </P>
                        <P>
                            Dotknutými osobami sú fyzické osoby – návštevníci/používatelia webovej stránky <ExternalLink href={webUrl} />.
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Účel spracúvania osobných údajov:</strong>
                        </P>
                        <P>
                            Účelom spracúvania osobných údajov je poskytovať používateľom webovej stránky služby informačnej spoločnosti, zabezpečiť plynulú a
                            bezpečnú prevádzku webovej stránky, zlepšovať používateľskú skúsenosť a zlepšovať spôsob, ako zacieliť na vhodné publikum.
                        </P>
                        <P>
                            Účelom spracúvania <strong>nevyhnutných cookies</strong> je umožniť používateľom to, aby mohli túto webovú stránku prezerať a
                            využívať jej funkcie. Tieto súbory cookies neukladajú žiadne informácie o používateľoch, ktoré by sa dali použiť na marketing alebo
                            na zapamätanie si, čo si používatelia na internete pozerali.
                        </P>
                        <P>
                            Účelom spracúvania <strong>analytických cookies</strong> je realizovať analýzu spôsobu používania webovej stránky používateľom,
                            vytvárať reporty o návštevnosti webstránky, o počte návštevníkov webovej stránky, o počte interakcií či najnavštevovanejších
                            častiach webstránky. Tieto súbory cookies zbierajú informácie o tom, či sa používateľom zobrazili chybové hlásenia. Nezbierajú
                            informácie, na základe ktorých by bolo možné zistiť totožnosť používateľov. Všetko informácie sú anonymné. Používajú sa na zlepšenie
                            funkčnosti webových stránok.
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Právny základ spracúvania:</strong>
                        </P>
                        <P>
                            Právnym základom pre spracúvanie nevyhnutných cookies je <strong>zákonná povinnosť prevádzkovateľa</strong> v zmysle čl. 6 ods. 1
                            písm. c) GDPR: <em>„spracúvanie je nevyhnutné na splnenie zákonnej povinnosti prevádzkovateľa“</em>. Zákonná povinnosť vyplýva
                            ministerstvu z § 109 ods. 8 zákona č. 452/2021 Z. z. o elektronických komunikáciách v znení neskorších predpisov.
                        </P>
                        <P>
                            Právnym základom pre spracúvanie analytických cookies je <strong>súhlas dotknutej osoby</strong> v zmysle čl. 6 ods. 1 písm. a)
                            GDPR: <em>„dotknutá osoba vyjadrila súhlas so spracúvaním svojich osobných údajov na jeden alebo viaceré konkrétne účely“</em>.
                            Dotknutá osoba má právo kedykoľvek svoj súhlas odvolať, pričom jej osobné údaje budú v tomto prípade vymazané a nebude možné ich
                            naďalej využívať.
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Sprostredkovateľ, ktorý spracúva osobné údaje v mene prevádzkovateľa:</strong>
                        </P>
                        <P>Ministerstvo nevyužíva žiadneho sprostredkovateľa.</P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Sub-sprostredkovateľ, ktorý spracúva osobné údaje v mene sprostredkovateľa:</strong>
                        </P>
                        <P>Ministerstvo nevyužíva žiadneho sub-sprostredkovateľa.</P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Príjemcovia osobných údajov:</strong>
                        </P>
                        <P>
                            Webová stránka ministerstva poskytuje osobné údaje spoločnosti zabezpečujúcej správu webovej stránky a na analytické účely
                            spoločnosti Google LLC. Ministerstvo poskytuje osobné údaje aj subjektom, ktoré sú oprávnené vyžadovať si osobné údaje na základe
                            osobitných predpisov aj keď sa tieto subjekty nepovažujú za príjemcov v zmysle GDPR.
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Doba uchovávania osobných údajov:</strong>
                        </P>
                        <P>
                            Cookies sú ukladané od momentu získania maximálne po dobu 13 mesiacov alebo aj skôr, v prípade, ak sa zmenia okolnosti získavania
                            alebo ukladania cookies (napr. ak ministerstvo nastaví úplne novú cookies lištu, ministerstvo zmení spôsob získavania alebo
                            spracúvania cookies alebo ak používateľ zmaže všetky cookies vo svojom prehliadači).
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Prenos osobných údajov do tretej krajiny:</strong>
                        </P>
                        <P>
                            Prevádzkovateľ analytického nástroja spracúva cookies, čím sa vykonáva aj prenos osobných údajov do tretích krajín. Konkrétne v
                            prípade Google Analytics ide o prenos do Spojených štátov amerických (USA). Spracúvanie osobných údajov sa vykonáva na základe čl.
                            45 ods. 1 GDPR podľa rozhodnutia Európskej komisie 2023/1795. Rozhodnutie Európskej komisie nájdete tu:{' '}
                            <ExternalLink
                                href={
                                    'https://eur-lex.europa.eu/legal-content/SK/TXT/?uri=uriserv%3AOJ.L_.2023.231.01.0118.01.SLK&toc=OJ%3AL%3A2023%3A231%3ATOC'
                                }
                            >
                                Rozhodnutie Komisie 2023/1795
                            </ExternalLink>
                        </P>
                    </Item>

                    <Item>
                        <P>
                            <strong>Prenos osobných údajov medzinárodným organizáciám:</strong>
                        </P>
                        <P>Nie, nevykonáva sa.</P>
                    </Item>

                    <Item>
                        <P>
                            Osobné údaje <strong>nebudú</strong> použité na <strong>automatizované individuálne rozhodovanie</strong> vrátane{' '}
                            <strong>profilovania.</strong>
                        </P>
                    </Item>

                    <Item>
                        <P>
                            V súlade s čl. 15 až 22 GDPR je dotknutá osoba oprávnená prostredníctvom žiadosti si uplatniť nasledovné{' '}
                            <strong>práva dotknutej osoby:</strong>
                        </P>
                        <LetterList>
                            <li>právo na potvrdenie o spracúvaní osobných údajov,</li>
                            <li>právo na získanie prístupu k osobným údajom, vrátane relevantných informácií týkajúcich sa ich spracúvania,</li>
                            <li>právo na opravu nesprávnych a na doplnenie neúplných osobných údajov, ktoré sa jej týkajú,</li>
                            <li>právo na vymazanie osobných údajov, ktoré sa jej týkajú,</li>
                            <li>právo na obmedzenie spracúvania osobných údajov,</li>
                            <li>právo na prenosnosť osobných údajov,</li>
                            <li>
                                právo <strong>namietať</strong> spracúvanie osobných údajov,
                            </li>
                            <li>právo na neuplatňovanie rozhodovania založeného výlučne na automatizovanom individuálnom rozhodovaní, vrátane profilovania</li>
                        </LetterList>
                    </Item>

                    <Item>
                        <P>
                            Dotknutá osoba je oprávnená podať <strong>žiadosť o výkon práv dotknutej osoby</strong> u prevádzkovateľa alebo u jeho zodpovednej
                            osoby listom na adresu prevádzkovateľa, e-mailom na{' '}
                            <ExternalLink href="mailto:zodpovedna.osoba@mirri.gov.sk">zodpovedna.osoba@mirri.gov.sk</ExternalLink> alebo osobne na podateľni
                            prevádzkovateľa.
                        </P>
                    </Item>

                    <Item>
                        <P>
                            Dotknutá osoba je oprávnená kedykoľvek svoj <strong>súhlas so spracúvaním osobných údajov</strong> odvolať:
                        </P>
                        <LetterList>
                            <li>
                                ak chcete odstrániť súbory cookies uložené vo Vašich zariadeniach, môžete to urobiť pomocou nastavení Vášho webového
                                prehliadača. Obvykle je potrebné kliknúť na tlačidlo „Nastavenia“ a následne na tlačidlo „Súbory cookie a povolenia
                                lokality“/„Ochrana súkromia a zabezpečenie“ a následne „Súbory cookie tretích strán“,
                            </li>
                            <li>
                                v pätičke webovej stránky <ExternalLink href={webUrl} /> v časti Cookies, klikom na tlačidlo Cookies si viete cookies lištu
                                kedykoľvek vyvolať a udeliť/odvolať súhlas so spracúvaním osobných údajov alebo nastaviť vlastné preferencie cookies.
                            </li>
                        </LetterList>
                    </Item>

                    <Item>
                        <P>
                            <strong>Informácia o zmluvnej alebo zákonnej požiadavke:</strong>
                        </P>
                        <P>
                            Poskytovanie nevyhnutných osobných údajov je zákonnou požiadavkou: vyplýva zo zákonnej povinnosti prevádzkovateľa podľa § 109 ods. 8
                            zákona č. 452/2021 Z.z. o elektronických komunikáciách v znení neskorších predpisov. V prípade neposkytnutia nevyhnutných osobných
                            údajov nie je možné prehliadať webovú stránku <ExternalLink href={webUrl} />.
                        </P>
                    </Item>

                    <Item>
                        <P>
                            Ak sa dotknutá osoba domnieva, že pri spracúvaní osobných údajov boli porušené jej práva, má právo podať návrh na začatie konania o
                            ochrane osobných údajov <strong>Úradu na ochranu osobných údajov SR</strong> v súlade s ustanoveniami zákona č. 18/2018 Z. z. o
                            ochrane osobných údajov a o zmene a doplnení niektorých zákonov v znení neskorších predpisov.{' '}
                            <ExternalLink href="https://dataprotection.gov.sk/sk/ine/vzory-formulare-stiahnutie/navrh-zacatie-konania-ochrane-osobnych-udajov/">
                                Návrh na začatie konania o ochrane osobných údajov | Úrad na ochranu osobných údajov Slovenskej republiky (gov.sk)
                            </ExternalLink>
                        </P>
                    </Item>
                </ol>
            </MainContent>
        </>
    );
}
