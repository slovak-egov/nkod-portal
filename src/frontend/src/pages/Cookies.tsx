import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useDocumentTitle } from '../client';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import Checkbox from '../components/Checkbox';
import InsetText from '../components/InsetText';
import Button from '../components/Button';
import { cookiesProcessingPath, useCookieConsent } from '../cookies';

export default function Cookies() {
    const { t } = useTranslation();
    useDocumentTitle('Nastavenia cookies');

    const { policy, savePolicy } = useCookieConsent();
    const [analytics, setAnalytics] = useState<boolean>(policy?.analytics ?? false);
    const [saved, setSaved] = useState<boolean>(false);
    const savedRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (saved) {
            savedRef.current?.focus();
        }
    }, [saved]);

    function save() {
        savePolicy(analytics, false);
        setSaved(true);
    }

    return (
        <>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: 'Nastavenia cookies' }]} />
            <MainContent>
                <PageHeader>Nastavenia cookies</PageHeader>

                {saved ? (
                    <div className="govuk-inset-text" role="alert" tabIndex={-1} ref={savedRef}>
                        <p className="govuk-body govuk-!-font-weight-bold">Vaše nastavenia cookies boli uložené.</p>
                        <p className="govuk-body">
                            {analytics ? 'Analytické súbory cookie sú povolené.' : 'Analytické súbory cookie sú zakázané.'} Túto voľbu môžete kedykoľvek zmeniť
                            na tejto stránke.
                        </p>
                    </div>
                ) : policy === null ? (
                    <p className="govuk-body">Zatiaľ ste nevyjadrili súhlas ani nesúhlas s používaním dodatočných súborov cookie.</p>
                ) : null}

                <p className="govuk-body">
                    Viac informácií nájdete v časti{' '}
                    <Link className="govuk-link" to={cookiesProcessingPath}>
                        Spracúvanie cookies na webovej stránke Národného katalógu otvorených dát
                    </Link>
                    .
                </p>

                <div>
                    <div>
                        <Checkbox label="Nevyhnutne nutné súbory cookie" checked disabled onCheckedChange={() => {}} />
                        <InsetText>
                            <p className="govuk-body">
                                Sú to základné súbory cookie, ktoré umožňujú pohybovať sa po webovej stránke a používať jej funkcie. Tieto súbory cookie
                                neukladajú žiadne informácie o vás, ktoré by sa dali použiť na marketing alebo na zapamätanie si, čo ste si na internete
                                pozerali.
                            </p>
                        </InsetText>
                    </div>
                    <div>
                        <Checkbox
                            label="Analytické súbory cookie"
                            checked={analytics}
                            onCheckedChange={(v) => {
                                setAnalytics(v);
                                setSaved(false);
                            }}
                        />
                        <InsetText>
                            <p className="govuk-body">
                                Tieto súbory zbierajú informácie o tom, ako sa používala webová stránka, napríklad ktoré stránky najčastejšie navštevujete a či
                                sa vám zobrazili chybové hlásenia. Nezbierajú informácie, na základe ktorých by bolo možné zistiť vašu totožnosť. Všetky
                                informácie sú anonymné. Používajú sa na zlepšenie funkčnosti webových stránok.
                            </p>
                        </InsetText>
                    </div>
                </div>
                <Button type="button" onClick={save}>
                    Uložiť nastavenia
                </Button>
            </MainContent>
        </>
    );
}
