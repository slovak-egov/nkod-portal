import { useTranslation } from 'react-i18next';
import Breadcrumbs from './Breadcrumbs';
import MainContent from './MainContent';
import PageHeader from './PageHeader';
import { useDocumentTitle } from '../client';
import Button from './Button';

type Props = {
    invitationToken: string;
};

export default function Invitation(props: Props) {
    const { t } = useTranslation();
    useDocumentTitle(t('invitationWasCreated'));

    const url = new URL('/pozvanka?' + props.invitationToken, document.baseURI);

    return (
        <div>
            <Breadcrumbs items={[{ title: t('nkod'), link: '/' }, { title: t('userList'), link: '/sprava/pouzivatelia' }, { title: t('newUser') }]} />
            <MainContent>
                <PageHeader>{t('invitationWasCreated')}</PageHeader>
            </MainContent>

            <p className="govuk-body">
                Pozvánka bola pre používateľa úspešne vytvorená. Na registráciu môže používateľ aplikovať tento vygenerovaný odkaz: {url.href}
            </p>

            <p className="govuk-body">
                <Button
                    onClick={() => {
                        navigator.clipboard.writeText(url.href);
                    }}
                >
                    Skopírovať odkaz
                </Button>
            </p>
        </div>
    );
}
