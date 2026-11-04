import { PropsWithChildren } from 'react';
import { useTranslation } from 'react-i18next';
import { useDocumentTitle } from '../client';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import { ExternalLink, P } from '../components/LegalText';

export const personalDataProtectionPath = '/ochrana-osobnych-udajov';
const title = 'Spracúvanie osobných údajov v Národnom katalógu otvorených dát';

function Heading(props: PropsWithChildren) {
    return <h2 className="govuk-heading-m">{props.children}</h2>;
}

export default function PersonalDataProtection() {
    const { t } = useTranslation();
    useDocumentTitle('Ochrana osobných údajov');

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: 'Ochrana osobných údajov' }]} />
            <MainContent>
                <PageHeader size="l">{title}</PageHeader>

                <Heading>Čo sú osobné údaje?</Heading>
                <P>
                    Osobné údaje v zmysle článku 4 ods. 1 Nariadenia Európskeho parlamentu a Rady (EÚ) 2016/679 z 27. apríla 2016 o ochrane fyzických osôb pri
                    spracúvaní osobných údajov a o voľnom pohybe takýchto údajov, ktorým sa zrušuje smernica 95/46/ES (všeobecné nariadenie o ochrane údajov –
                    ďalej len „GDPR“) sú akékoľvek informácie týkajúce sa identifikovanej alebo identifikovateľnej fyzickej osoby. Identifikovateľná fyzická
                    osoba je osoba, ktorú možno identifikovať priamo alebo nepriamo, najmä odkazom na identifikátor, ako je meno, identifikačné číslo,
                    lokalizačné údaje, online identifikátor, alebo odkazom na jeden, či viaceré prvky, ktoré sú špecifické pre fyzickú, fyziologickú, genetickú,
                    mentálnu, ekonomickú, kultúrnu alebo sociálnu identitu tejto fyzickej osoby.
                </P>

                <Heading>Kto je zodpovedný na spracúvanie Vašich osobných údajov na tomto webovom sídle?</Heading>
                <P>
                    Prevádzkovateľom podľa GDPR je Ministerstvo investícií, regionálneho rozvoja a informatizácie Slovenskej republiky (ďalej len
                    ,,ministerstvo“). Ministerstvo môžete kontaktovať na adrese:
                </P>
                <P>
                    Pribinova 25
                    <br />
                    811 09 Bratislava
                    <br />
                    <ExternalLink href="https://mirri.gov.sk/kontakt/" />
                </P>

                <Heading>Aké sú kontaktné údaje zodpovednej osoby za ochranu osobných údajov na MIRRI SR?</Heading>
                <P>
                    Ak máte akékoľvek otázky ohľadom toho, ako spracúvame Vaše osobné údaje alebo si chcete uplatniť Vaše práva v súvislosti s ochranou osobných
                    údajov, môžete kontaktovať zodpovednú osobu za ministerstvo:
                </P>
                <ul className="govuk-list govuk-list--bullet">
                    <li>
                        e-mail: <ExternalLink href="mailto:zodpovedna.osoba@mirri.gov.sk">zodpovedna.osoba@mirri.gov.sk</ExternalLink>
                    </li>
                    <li>telefónne číslo: + 421 2 2092 8425</li>
                </ul>

                <Heading>Na aké účely spracúvame Vaše osobné údaje?</Heading>
                <P>
                    Vaše osobné údaje spracúvame, aby sme Vás mohli zaregistrovať ako člena OpenData komunity a vytvoriť Vám účet; aby ste mohli zadávať podnety
                    na zverejnenie otvorených údajov, registrovať aplikáciu, ktorá používa otvorené údaje, zadávať komentáre a lajky, nahlasovať nám chyby a
                    pripomienky a tiež, aby sme Vám mohli poskytnúť technickú podporu, ak ju budete potrebovať.
                </P>
                <P>Vaše meno a priezvisko používame na identifikáciu zadávateľa konkrétneho podnetu.</P>
                <P>Osobné údaje spracúvame aj na zlepšovanie kvality poskytovaných služieb pre používateľov a získavanie spätnej väzby od používateľov.</P>

                <Heading>Aké kategórie osobných údajov spracúvame?</Heading>
                <P>Nespracúvame o Vás žiadne citlivé údaje. Spracúvame iba bežné osobné údaje, a to Vaše meno, priezvisko a login – e-mailovú adresu.</P>
                <P>
                    V prípade, ak využijete na prihlásenie na náš portál jednu z možností, ktoré ponúka Ústredný portál verejnej správy{' '}
                    <ExternalLink href="https://www.slovensko.sk/" />, budeme okrem Vášho mena, priezviska a e-mailovej adresy spracúvať aj Váš identifikátor
                    identity v module Ústredného portálu verejnej správy a Váš titul k menu a priezvisku. V prípade, ak vystupujete ako zástupca niektorej z
                    organizácií, spracúvame aj údaj o type zastupovania.
                </P>
                <P>
                    V prípade, ak využijete na prihlásenie na náš portál prihlásenie prostredníctvom existujúceho Google účtu, budeme o Vás spracúvať profilové
                    meno a priezvisko a e mailovú adresu.
                </P>

                <Heading>Na akom právnom základe spracúvame Vaše osobné údaje?</Heading>
                <P>
                    Právnym základom pre spracúvanie osobných údajov z registračného formulára je <strong>plnenie zmluvného vzťahu</strong> medzi ministerstvom
                    a používateľom, ktorý vzniká pri odsúhlasení podmienok používania účtu a oboznámení sa s podmienkami ochrany osobných údajov v zmysle čl. 6
                    ods. 1 písm. b) GDPR:{' '}
                    <em>
                        „spracúvanie je nevyhnutné na plnenie zmluvy, ktorej zmluvnou stranou je dotknutá osoba, alebo aby sa na základe žiadosti dotknutej
                        osoby vykonali opatrenia pred uzatvorením zmluvy“.
                    </em>
                </P>
                <P>
                    Použitie Vášho mena a priezviska na účely identifikácie zadávateľa podnetu vykonáva ministerstvo na základe právneho základu{' '}
                    <strong>oprávnených záujmov prevádzkovateľa</strong> podľa čl. 6 ods.1. písm. f) GDPR:{' '}
                    <em>
                        „spracúvanie je nevyhnutné na účely oprávnených záujmov, ktoré sleduje prevádzkovateľ alebo tretia strana, s výnimkou prípadov, keď nad
                        takýmito záujmami prevažujú záujmy alebo základné práva a slobody dotknutej osoby, ktoré si vyžadujú ochranu osobných údajov, najmä ak
                        je dotknutou osobu dieťa.“
                    </em>
                </P>
                <P>
                    Oprávneným záujmom je zvýšiť relevantnosť, transparentnosť a kvalitu podnetu tým, že bude zadávateľ podnetu identifikovaný na základe mena a
                    priezviska.
                </P>
                <P>Ďalším oprávneným záujmom je zlepšovať kvalitu poskytovaných služieb pre používateľov a získavanie spätnej väzby od používateľov.</P>

                <Heading>Ako dlho Vaše osobné údaje spracúvame?</Heading>
                <P>
                    Vaše osobné údaje z používateľského účtu a osobné údaje na identifikáciu podnetu spracúvame až pokým aktívne nepožiadate o zrušenie svojho
                    účtu.
                </P>

                <Heading>Kto bude mať prístup k Vašim osobným údajom?</Heading>
                <P>
                    K Vašim osobným údajom budú mať prístup iba poverení zamestnanci ministerstva, lokálni administrátori a dodávateľ webového riešenia portálu
                    otvorených údajov.
                </P>

                <Heading>Komu môžu byť Vaše osobné údaje poskytnuté?</Heading>
                <P>
                    Ak využijete jednu z možností prihlásenia cez Ústredný portál verejnej správy, Vaše osobné údaje z overenia Vašej identity môže spracúvať
                    Národná agentúra pre sieťové a elektronické služby. Ak využijete možnosť prihlásenia cez existujúci účet Google, Vaše osobné údaje z
                    overenia Vašej identity môže spracúvať spoločnosť Google. Zo strany MIRRI SR Vaše osobné údaje neposkytujeme žiadnym ďalším subjektom.
                </P>

                <Heading>O kom zhromažďujeme a spracúvame osobné údaje?</Heading>
                <P>
                    Dotknuté osoby, o ktorých získavame, zhromažďujeme a ďalej spracúvame osobné údaje, sú registrovaní členovia OpenData komunity (používatelia
                    účtu).
                </P>

                <Heading>Budú Vaše osobné údaje prenášané do tretích krajín alebo medzinárodnej organizácii?</Heading>
                <P>Nie, Vaše osobné údaje nebudeme poskytovať tretím krajinám ani medzinárodným organizáciám.</P>

                <Heading>Zahŕňa spracúvanie Vašich osobných údajov ministerstvom automatizované individuálne rozhodovanie alebo profilovanie?</Heading>
                <P>
                    Nie. Nebudeme Vás ,,škatuľkovať“ (profilovať), nebudeme sledovať ani hodnotiť Vaše správanie ani z dát nebudeme vyvodzovať žiadne závery,
                    ktoré by sa Vás negatívne dotkli.
                </P>

                <Heading>Je spracúvanie osobných údajov zákonnou alebo zmluvnou požiadavkou?</Heading>
                <P>
                    Pred začatím používania účtu je potrebné poskytnúť osobné údaje, ktoré sú nevyhnutné na založenie Vášho účtu. Nie je to zákonná povinnosť
                    poskytnúť svoje osobné údaje, ale je to požiadavka pred začatím používania registrovaného účtu. V prípade, že by ste neposkytli potrebné
                    osobné údaje (meno, priezvisko a login/e-mail), nemohli by ste účet používať a nemohli by sme Vám ani poskytnúť prihlasovacie údaje do Vášho
                    účtu. Na prihlásenie môžete využiť aj iné spôsoby mimo nášho webu, a to prihlásenie cez vytvorený Google účet alebo s pomocou eID
                    karty/mobilného kľúča/európskeho dokladu.
                </P>

                <Heading>Aké sú Vaše práva v oblasti GDPR a ako si ich môžete uplatniť?</Heading>
                <P>Máte nárok na všetky práva podľa GDPR:</P>
                <ul className="govuk-list govuk-list--bullet">
                    <li>právo získať prístup k svojim osobným údajom,</li>
                    <li>právo dostať informácie o spracúvaní osobných údajov,</li>
                    <li>právo dostať potvrdenie od ministerstva, že spracúvame Vaše osobné údaje,</li>
                    <li>právo na opravu a doplnenie osobných údajov,</li>
                    <li>právo na vymazanie svojich osobných údajov (právo ,,na zabudnutie“),</li>
                    <li>právo na obmedzenie spracúvania osobných údajov,</li>
                    <li>právo preniesť si osobné údaje od jednej spoločnosti k druhej,</li>
                    <li>
                        právo <strong>namietať</strong> voči spracúvaniu osobných údajov a
                    </li>
                    <li>právo na to, aby sa na Vás nevzťahovalo rozhodnutie, ktoré by vzniklo z profilovania alebo automatizovaného spracúvania.</li>
                </ul>
                <P>Všetky práva podľa GDPR si môžete uplatniť poštou, podaním na podateľni MIRRI SR alebo odoslaním e-mailovej žiadosti.</P>

                <Heading>Na koho sa môžete obrátiť, ak došlo k porušeniu Vašich práv podľa GDPR?</Heading>
                <P>
                    Ak sa ako dotknutá osoba domnievate, že pri spracúvaní osobných údajov boli porušené Vaše práva, máte právo podať návrh na začatie konania o
                    ochrane osobných údajov <strong>Úradu na ochranu osobných údajov SR</strong> v súlade s ustanoveniami zákona č. 18/2018 Z. z. o ochrane
                    osobných údajov a o zmene a doplnení niektorých zákonov v znení neskorších predpisov.{' '}
                    <ExternalLink href="https://dataprotection.gov.sk/sk/dotknute-osoby/konanie-ochrane-osobnych-udajov/">
                        Konanie o ochrane osobných údajov
                    </ExternalLink>
                </P>
                <P>
                    E-mail: <ExternalLink href="mailto:statny.dozor@pdp.gov.sk">statny.dozor@pdp.gov.sk</ExternalLink>
                    <br />
                    Webové sídlo: <ExternalLink href="https://dataprotection.gov.sk/" />
                </P>
            </MainContent>
        </>
    );
}
