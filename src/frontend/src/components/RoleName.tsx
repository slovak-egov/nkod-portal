type Props = {
    role: string|null;
}

export default function RoleName(props: Props)
{
    let name = props.role;

    switch (name) {
        case 'Publisher':
            name = 'Zverejňovateľ';
            break;
        case 'PublisherAdmin':
                name = 'Administrátor poskytovateľa';
                break;
    }

    return <>{name ?? 'žiadna'}</>
}