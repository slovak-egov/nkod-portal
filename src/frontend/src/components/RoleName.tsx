import { useTranslation } from "react-i18next";

type Props = {
    role: string|null;
}

export default function RoleName(props: Props)
{
    let name = props.role;
    const {t} = useTranslation();

    switch (name) {
        case 'Publisher':
            name = t('publisherUser');
            break;
        case 'PublisherAdmin':
                name = t('publisherAdmin');
                break;
    }

    return <>{name ?? t('noRole')}</>
}