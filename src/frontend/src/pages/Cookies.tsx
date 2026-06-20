import { useTranslation } from 'react-i18next';
import { useDocumentTitle } from '../client';
import Breadcrumbs from '../components/Breadcrumbs';
import MainContent from '../components/MainContent';
import PageHeader from '../components/PageHeader';
import Checkbox from '../components/Checkbox';
import InsetText from '../components/InsetText';
import Button from '../components/Button';

export default function Cookies() {
    const { t } = useTranslation();

    return (
        <>
            <MainContent>
                <PageHeader>Nastavenia cookies</PageHeader>

                <div>
                    <Checkbox label="Nevyhnutne nutné súbory cookie" checked disabled onCheckedChange={() => {}} />
                    <InsetText>
                        <p className="govuk-body">
                            Sú to základné súbory cookie, ktoré umožňujú pohybovať sa po webovej stránke a používať jej funkcie. Tieto súbory cookie neukladajú
                            žiadne informácie o vás, ktoré by sa dali použiť na marketing alebo na zapamätanie si, čo ste si na internete pozerali.
                        </p>
                    </InsetText>
                </div>
                <div>
                    <Checkbox label="Analytické súbory cookie" checked onCheckedChange={() => {}} />
                    <InsetText>
                        <p className="govuk-body">
                            Tieto súbory zbierajú informácie o tom, ako sa používala webová stránka, napríklad ktoré stránky najčastejšie navštevujete a či sa
                            vám zobrazili chybové hlásenia. Nezbierajú informácie, na základe ktorých by bolo možné zistiť vašu totožnosť. Všetky informácie sú
                            anonymné. Používajú sa na zlepšenie funkčnosti webových stránok.
                        </p>
                    </InsetText>
                </div>
                <Button>Uložiť nastavenia</Button>
            </MainContent>
        </>
    );
}
